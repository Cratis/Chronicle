// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Transactions;

public class when_inventorying_protected_surfaces : Specification
{
    static readonly (Type Surface, Type Implementation)[] _surfaces =
    [
        (typeof(IEventStore), typeof(EventStore)),
        (typeof(IEventSequence), typeof(EventSequence)),
        (typeof(IEventLog), typeof(EventLog)),
        (typeof(IReadModels), typeof(ReadModels.ReadModels)),
        (typeof(IMaterializedReadModels), typeof(MaterializedReadModels)),
        (typeof(IProjections), typeof(Projections.Projections)),
        (typeof(IReducers), typeof(Reducers.Reducers)),
        (typeof(IDecisionReads), typeof(DecisionReads))
    ];

    /// <summary>
    /// Keys include generic arity and argument count; the overload-count spec also pins
    /// allowances that have multiple overloads with the same key.
    /// Only metadata, administration and protected acquisition are exempt from raw-read refusal.
    /// </summary>
    static readonly IReadOnlyDictionary<string, string> _allowed = BuildAllowances();
    readonly List<string> _unguarded = [];

    async Task Because()
    {
        var unit = new UnitOfWork(CorrelationId.New(), _ => { }, Substitute.For<IEventStore>());
        using var frame = unit.EnforceReadProtection();
        foreach (var (surface, implementation) in _surfaces)
        {
            // Deliberately no dependencies: a public guard must precede transport, serialization
            // or access to state. A missing guard fails with a diagnostic instead of doing I/O.
            var instance = RuntimeHelpers.GetUninitializedObject(implementation);
            foreach (var method in Members(surface))
            {
                if (_allowed.TryGetValue(Key(method), out var reason))
                {
                    if (string.IsNullOrWhiteSpace(reason)) _unguarded.Add($"{method}: missing allowance reason");
                    continue;
                }
                var error = await Invoke(instance, method);
                if (error is not ReadRefusedWhileReadsAreProtected and not DetachedReadWhileReadsAreProtected and not ImmediateAppendWhileReadsAreProtected and not ReadProtectionUnitMismatch)
                {
                    _unguarded.Add($"{surface.Name}.{method}: {error?.GetType().Name ?? "not refused"}");
                }
            }
        }
    }

    [Fact] void should_guard_or_explicitly_allow_every_public_member() => _unguarded.ShouldBeEmpty();

    [Fact] void should_keep_allowances_tied_to_existing_members() =>
        _allowed.Keys.Except(_surfaces.SelectMany(_ => Members(_.Surface)).Select(Key)).ShouldBeEmpty();

    [Fact] void should_require_review_of_new_allowed_overloads()
    {
        var overloads = _surfaces.SelectMany(_ => Members(_.Surface)).Distinct().GroupBy(Key).ToDictionary(_ => _.Key, _ => _.Count());
        foreach (var key in _allowed.Keys)
        {
            var expected = key == "IProjections.HasFor`0/1" || key == "IReadModels.Release`1/1" || key == "IReducers.Replay`0/1" ? 2 : 1;
            overloads[key].ShouldEqual(expected);
        }
    }

    static IEnumerable<MethodInfo> Members(Type surface) =>
        surface.GetInterfaces().Append(surface).SelectMany(_ => _.GetMethods()).Distinct();

    static string Key(MethodInfo method) => $"{method.DeclaringType!.Name}.{method.Name}`{method.GetGenericArguments().Length}/{method.GetParameters().Length}";

    static async Task<Exception?> Invoke(object instance, MethodInfo method)
    {
        if (method.IsGenericMethodDefinition) method = method.MakeGenericMethod(Enumerable.Repeat(typeof(object), method.GetGenericArguments().Length).ToArray());
        var arguments = method.GetParameters().Select(parameter =>
        {
            if (parameter.ParameterType == typeof(IUnitOfWork)) return Substitute.For<IUnitOfWork>();
            return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
        }).ToArray();
        try
        {
            if (method.Invoke(instance, arguments) is Task task) await task;
            return null;
        }
        catch (TargetInvocationException exception)
        {
            return exception.InnerException;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    static IReadOnlyDictionary<string, string> BuildAllowances()
    {
        var allowed = new Dictionary<string, string>();
        Add(
            "Store identity, services and local registration metadata; no event or read-model state is read.",
            "IEventStore.get_Name`0/0",
            "IEventStore.get_Namespace`0/0",
            "IEventStore.get_Connection`0/0",
            "IEventStore.get_UnitOfWorkManager`0/0",
            "IEventStore.get_EventTypes`0/0",
            "IEventStore.get_EventSources`0/0",
            "IEventStore.get_Constraints`0/0",
            "IEventStore.get_EventLog`0/0",
            "IEventStore.get_Jobs`0/0",
            "IEventStore.get_Reactors`0/0",
            "IEventStore.get_Occurrences`0/0",
            "IEventStore.get_Reducers`0/0",
            "IEventStore.get_Projections`0/0",
            "IEventStore.get_Webhooks`0/0",
            "IEventStore.get_ExternalServices`0/0",
            "IEventStore.get_Subscriptions`0/0",
            "IEventStore.get_Observers`0/0",
            "IEventStore.get_FailedPartitions`0/0",
            "IEventStore.get_ReadModels`0/0",
            "IEventStore.get_ReadModelReactors`0/0",
            "IEventStore.get_Seeding`0/0",
            "IEventStore.get_Patterns`0/0",
            "IEventStore.get_PII`0/0",
            "IEventStore.get_Identities`0/0",
            "IEventStore.get_Registration`0/0",
            "IEventStore.get_Discovery`0/0",
            "IEventStore.GetEventSequence`0/1");
        Add(
            "Artifact discovery and registration are administration, not command decision state.",
            "IEventStore.DiscoverAll`0/0",
            "IEventStore.RegisterAll`0/0",
            "IEventStore.RegisterDefinitions`0/2",
            "IEventStore.RemoveNamespace`0/2",
            "IReadModels.Register`0/0",
            "IReadModels.Register`1/0",
            "IProjections.Discover`0/0",
            "IProjections.Register`0/0",
            "IProjections.Register`1/0",
            "IProjections.Register`1/2",
            "IReducers.Discover`0/0",
            "IReducers.Register`0/0",
            "IReducers.Register`2/0",
            "IReducers.Register`0/1");
        Add(
            "Sequence identity, append notifications, staging accessor and local serialization do not acquire committed state.",
            "IEventSequence.get_Id`0/0",
            "IEventSequence.get_SupportsIdempotentAppends`0/0",
            "IEventSequence.get_AppendOperations`0/0",
            "IEventSequence.get_Transactional`0/0",
            "IEventSequence.Prepare`0/1");
        Add("Materialized accessor obtains a guarded service, not model state.", "IReadModels.get_Materialized`0/0");
        Add(
            "Session cleanup and release of caller-supplied content do not acquire Chronicle decision state.",
            "IReadModels.DehydrateSession`0/3",
            "IReadModels.Release`1/1");
        Add(
            "Local artifact shape and handler lookup, not event history or model state.",
            "IProjections.HasFor`0/1",
            "IProjections.HasFor`1/0",
            "IProjections.GetAllHandlers`0/0",
            "IProjections.GetHandlerFor`1/0",
            "IProjections.GetProjectionIdFor`1/0",
            "IProjections.GetProjectionIdForModel`1/0",
            "IProjections.GetProjectionIdForModel`0/1",
            "IProjections.GetExternalEventStoreSubscriptions`0/0",
            "IReducers.HasReducerFor`0/1",
            "IReducers.HasFor`0/1",
            "IReducers.HasFor`1/0",
            "IReducers.GetAllHandlers`0/0",
            "IReducers.GetHandlerById`0/1",
            "IReducers.GetHandlerForReadModelType`0/1",
            "IReducers.GetHandlerFor`0/1",
            "IReducers.GetHandlerFor`1/0",
            "IReducers.GetClrType`0/1",
            "IDecisionReads.Admit`1/0",
            "IDecisionReads.AdmitForStream`1/0",
            "IDecisionReads.Describe`1/0");
        Add(
            "Observer administration and diagnostics are not read-model decision state.",
            "IProjections.GetFailedPartitionsFor`1/0",
            "IProjections.GetFailedPartitionsFor`0/1",
            "IProjections.GetFailedPartitionsForModel`1/0",
            "IProjections.GetFailedPartitionsForModel`0/1",
            "IProjections.GetStateFor`1/0",
            "IProjections.GetStateForModel`1/0",
            "IProjections.GetStateForModel`0/1",
            "IProjections.Replay`1/0",
            "IProjections.Replay`0/1",
            "IProjections.FastForward`1/0",
            "IProjections.FastForward`0/1",
            "IReducers.GetFailedPartitionsFor`1/0",
            "IReducers.GetFailedPartitionsFor`0/1",
            "IReducers.GetStateFor`1/0",
            "IReducers.GetStateFor`0/1",
            "IReducers.Replay`1/0",
            "IReducers.Replay`0/1",
            "IReducers.FastForward`1/0",
            "IReducers.FastForward`0/1");
        Add(
            "Ambient protected acquisition delegates to GetFor and enrolls before returning; covered by acquisition specs.",
            "IDecisionReads.Get`1/2",
            "IDecisionReads.Get`1/5");
        return allowed;

        void Add(string reason, params string[] members)
        {
            foreach (var member in members) allowed.Add(member, reason);
        }
    }
}
