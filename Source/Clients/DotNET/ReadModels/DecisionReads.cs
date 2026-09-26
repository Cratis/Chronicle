// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Schemas;
using Grpc.Core;
using ProtoBuf.Grpc;
using ProjectionDefinition = Cratis.Chronicle.Contracts.Projections.ProjectionDefinition;

namespace Cratis.Chronicle.ReadModels;

/// <summary>Client-side implementation of decision-consistent reads.</summary>
public sealed class DecisionReads : IDecisionReads
{
    static readonly ConditionalWeakTable<IEventStore, IDecisionReads> _instances = new();
    readonly IEventStore _eventStore;
    readonly Projections.Projections _projections;
    readonly IJsonSchemaGenerator _schemas;
    readonly JsonSerializerOptions _jsonOptions;
    readonly ConcurrentDictionary<Type, (DecisionReadAdmission Admission, ProjectionDefinition? Definition, EventType[] Types, bool GuidKey)> _admissions = new();
    readonly ConcurrentDictionary<Type, Task> _agreements = new();

    /// <summary>Creates a decision reader for a concrete event store.</summary>
    /// <param name="eventStore">The target event store.</param>
    /// <param name="projections">The registered projections.</param>
    /// <param name="schemas">The schema generator.</param>
    /// <param name="jsonOptions">The JSON serializer options.</param>
    internal DecisionReads(IEventStore eventStore, Projections.Projections projections, IJsonSchemaGenerator schemas, JsonSerializerOptions jsonOptions)
    {
        _eventStore = eventStore;
        _projections = projections;
        _schemas = schemas;
        _jsonOptions = jsonOptions;
        projections.Registered += () =>
        {
            _agreements.Clear();
            _admissions.Clear();
        };
    }

    /// <inheritdoc/>
    public DecisionReadAdmission Admit<T>()
        where T : class =>
        _admissions.GetOrAdd(typeof(T), Assess).Admission;

    /// <inheritdoc/>
    public async Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!_eventStore.UnitOfWorkManager.HasCurrent) throw new DecisionReadRequiresUnitOfWork();
        var unitOfWork = _eventStore.UnitOfWorkManager.Current;
        var read = await GetDetached<T>(key, cancellationToken);
        unitOfWork.AddDecisionRead(read);
        return read;
    }

    /// <inheritdoc/>
    public async Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        var type = typeof(T);
        var (definition, types) = GetAdmittedShape<T>(key);
        cancellationToken.ThrowIfCancellationRequested();
        var agreement = _agreements.GetOrAdd(
            type,
            static (modelType, state) => state.Self.CheckAgreement(modelType, state.Definition, state.Types),
            (Self: this, Definition: definition, Types: types));
        try
        {
            await agreement.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is not DecisionReadRefused)
        {
            ((ICollection<KeyValuePair<Type, Task>>)_agreements).Remove(new(type, agreement));
            throw;
        }
        var services = ((IChronicleServicesAccessor)_eventStore.Connection).Services;
        var callContext = new CallContext(new CallOptions(cancellationToken: cancellationToken));
        var source = (EventSourceId)key;
        for (var attempt = 0; attempt != 3; ++attempt)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Capture both tails before starting the fold. The narrow tail is only a diagnostic;
            // the unfiltered tail is the boundary of the concurrency scope.
            var boundaryTask = services.Sequences.TailSequenceNumber(
                new() { EventStore = _eventStore.Name, Namespace = _eventStore.Namespace, EventSequenceId = EventSequenceId.Log },
                callContext);
            var probeTask = services.Sequences.TailSequenceNumber(
                new()
                {
                    EventStore = _eventStore.Name, Namespace = _eventStore.Namespace, EventSequenceId = EventSequenceId.Log,
                    EventSourceId = source.Value, EventTypeIds = string.Join(',', types.Select(_ => _.Id.Value))
                },
                callContext);
            await Task.WhenAll(boundaryTask, probeTask);
            var boundary = (EventSequenceNumber)(await boundaryTask).EnsureSuccess().SequenceNumber;
            var probe = (EventSequenceNumber)(await probeTask).EnsureSuccess().SequenceNumber;
            var session = Guid.NewGuid();
            GetInstanceByKeyResponse result;
            try
            {
                result = await services.ReadModels.GetInstanceByKey(
                    new()
                    {
                        EventStore = _eventStore.Name,
                        Namespace = _eventStore.Namespace,
                        ReadModelIdentifier = type.GetReadModelIdentifier(),
                        EventSequenceId = EventSequenceId.Log,
                        ReadModelKey = key,
                        SessionId = session.ToString()
                    },
                    callContext);
            }
            finally
            {
                // Cleanup is advisory and must not mask the read result. Observe errors even when
                // the server has already deactivated the session.
                _ = Dehydrate(session, type, key, cancellationToken);
            }
            var last = (EventSequenceNumber)result.LastHandledEventSequenceNumber;
            if (probe.IsActualValue && (!last.IsActualValue || last.Value < probe.Value))
            {
                if (attempt < 2) continue;
                throw new DecisionReadRefused(DecisionReadRefusalReason.FoldIncomplete, type);
            }
            if (last.IsActualValue && (!boundary.IsActualValue || last.Value > boundary.Value) && attempt < 2) continue;
            var instance = !last.IsActualValue || result.ReadModel == "null"
                ? null
                : JsonSerializer.Deserialize<T>(result.ReadModel, _jsonOptions);
            return new DecisionRead<T>(
                key,
                instance,
                _eventStore.Name,
                _eventStore.Namespace,
                boundary.IsUnavailable ? EventSequenceNumber.BeforeFirst : boundary,
                types);
        }
        throw new InvalidOperationException("Unreachable decision read retry state.");
    }

    /// <summary>Registers an in-process testing reader for the target store.</summary>
    /// <param name="eventStore">The in-process store.</param>
    /// <param name="reader">The testing reader.</param>
    internal static void RegisterTesting(IEventStore eventStore, IDecisionReads reader) => _instances.Add(eventStore, reader);

    /// <summary>Gets the decision reader associated with the event store.</summary>
    /// <param name="eventStore">The target event store.</param>
    /// <returns>The decision reader.</returns>
    /// <exception cref="InvalidOperationException">The event store does not support decision reads.</exception>
    internal static IDecisionReads For(IEventStore eventStore)
    {
        if (_instances.TryGetValue(eventStore, out var registered))
        {
            return registered;
        }
        if (eventStore is EventStore)
        {
            return _instances.GetValue(eventStore, static key =>
            {
                var concrete = (EventStore)key;
                return new DecisionReads(key, (Projections.Projections)concrete.Projections, concrete.DecisionReadSchemas, concrete.DecisionReadJsonOptions);
            });
        }
        throw new InvalidOperationException("This event store does not support decision reads.");
    }

    /// <summary>Gets the definition and its event types, enforcing the same admission and key checks in testing.</summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="key">The raw source key.</param>
    /// <returns>The admitted definition and its types.</returns>
    /// <exception cref="DecisionReadRefused">The shape or key is not safe to guard.</exception>
    internal (ProjectionDefinition Definition, EventType[] Types) GetAdmittedShape<T>(ReadModelKey key)
        where T : class
    {
        var type = typeof(T);
        var (admission, definition, types, guidKey) = _admissions.GetOrAdd(type, Assess);
        if (!admission.IsAdmitted)
        {
            throw new DecisionReadRefused(admission.Reason!.Value, type);
        }
        if (key is null || string.IsNullOrWhiteSpace(key.Value) || key.Value == "*" ||
            key.Value.Contains('#') || key.Value != key.Value.Trim() ||
            (guidKey && (!Guid.TryParseExact(key.Value, "D", out var guid) || guid.ToString("D") != key.Value)))
        {
            throw new DecisionReadRefused(DecisionReadRefusalReason.InvalidKey, type);
        }
        return (definition!, types);
    }

    async Task Dehydrate(Guid session, Type type, ReadModelKey key, CancellationToken cancellationToken)
    {
        try
        {
            await ((IChronicleServicesAccessor)_eventStore.Connection).Services.ReadModels.DehydrateSession(
                new()
                {
                    EventStore = _eventStore.Name,
                    Namespace = _eventStore.Namespace,
                    ReadModelIdentifier = type.GetReadModelIdentifier(),
                    EventSequenceId = EventSequenceId.Log,
                    ReadModelKey = key,
                    SessionId = session.ToString()
                },
                new CallContext(new CallOptions(cancellationToken: cancellationToken)));
        }
        catch (Exception exception)
        {
            // A session is ephemeral; read and append outcomes are independent of deactivation.
            System.Diagnostics.Trace.TraceWarning("Could not dehydrate a decision read session: {0}", exception);
        }
    }

    (DecisionReadAdmission Admission, ProjectionDefinition? Definition, EventType[] Types, bool GuidKey) Assess(Type type)
    {
        static (DecisionReadAdmission, ProjectionDefinition?, EventType[], bool) Refuse(DecisionReadRefusalReason reason) =>
            (new(false, reason), null, [], false);
        if (_eventStore.Reducers.HasFor(type)) return Refuse(DecisionReadRefusalReason.Reducer);
        var matches = _projections.Definitions.Where(_ => _.ReadModel == type.GetReadModelIdentifier()).ToArray();
        if (matches.Length != 1) return Refuse(DecisionReadRefusalReason.AmbiguousProjection);
        var definition = matches[0];
        if (definition.EventSequenceId != EventSequenceId.Log.Value) return Refuse(DecisionReadRefusalReason.NotEventLog);
        if (definition.Join.Count != 0 || definition.RemovedWithJoin.Count != 0) return Refuse(DecisionReadRefusalReason.Join);
        if (definition.Children.Count != 0 || definition.Nested.Count != 0) return Refuse(DecisionReadRefusalReason.Hierarchy);
        if (definition.SubscribesToAllEvents) return Refuse(DecisionReadRefusalReason.OpenEndedEventTypes);
        if (definition.FromEvery.Count != 0) return Refuse(DecisionReadRefusalReason.Derivatives);
        if (definition.FromEventProperty is not null) return Refuse(DecisionReadRefusalReason.FromEventProperty);
        static bool IsSource(string? expression) => string.IsNullOrEmpty(expression) || expression == "$eventSourceId";
        if (definition.From.Values.Any(_ => !IsSource(_.Key) || !IsSource(_.ParentKey)) ||
            definition.RemovedWith.Values.Any(_ => !IsSource(_.Key) || !IsSource(_.ParentKey)) ||
            !IsSource(definition.All.Key))
        {
            return Refuse(DecisionReadRefusalReason.NotEventSourceKeyed);
        }
        var types = definition.From.Keys.Concat(definition.RemovedWith.Keys).DistinctBy(_ => _.Id).ToArray();
        if (types.Length == 0) return Refuse(DecisionReadRefusalReason.NoEventTypes);
        if (types.Any(_ => _.Id.Contains(','))) return Refuse(DecisionReadRefusalReason.UnsupportedEventTypeId);
        var schema = _schemas.Generate(type);
        if (!schema.HasKeyProperty()) return Refuse(DecisionReadRefusalReason.KeyConversion);
        var key = schema.GetKeyProperty();
        var format = key.ActualTypeSchema.Format;
        var target = key.GetTargetTypeForJsonSchemaProperty(new TypeFormats());
        if (target != typeof(string) && target != typeof(Guid)) return Refuse(DecisionReadRefusalReason.KeyConversion);
        if (target == typeof(string) && !string.IsNullOrEmpty(format)) return Refuse(DecisionReadRefusalReason.KeyConversion);
        if (target == typeof(Guid) && format != "guid" && format != "uuid") return Refuse(DecisionReadRefusalReason.KeyConversion);
        return (new(true, null), definition,
            types.Select(_ => new EventType(_.Id, _.Generation, _.Tombstone)).ToArray(), target == typeof(Guid));
    }

    async Task CheckAgreement(Type type, ProjectionDefinition client, EventType[] types)
    {
        var services = ((IChronicleServicesAccessor)_eventStore.Connection).Services;
        var context = CallContext.Default;
        var readModels = await services.ReadModels.GetDefinitions(new() { EventStore = _eventStore.Name }, context);
        var matches = readModels.ReadModels.Where(_ => _.Type.Identifier == type.GetReadModelIdentifier()).ToArray();
        var definitions = await services.Projections.GetAllDefinitions(new() { EventStore = _eventStore.Name }, context);
        var projections = definitions.Where(_ => _.Identifier == client.Identifier).ToArray();
        if (matches.Length != 1 || matches[0].ObserverIdentifier != client.Identifier ||
            matches[0].ObserverType != ReadModelObserverType.Projection || projections.Length != 1 ||
            projections[0].ReadModel != client.ReadModel ||
            projections[0].EventSequenceId != client.EventSequenceId ||
            projections[0].SubscribesToAllEvents != client.SubscribesToAllEvents ||
            projections[0].Join.Count != client.Join.Count || projections[0].RemovedWithJoin.Count != client.RemovedWithJoin.Count ||
            projections[0].Children.Count != client.Children.Count || projections[0].Nested.Count != client.Nested.Count ||
            projections[0].FromEvery.Count != client.FromEvery.Count ||
            projections[0].FromEventProperty is not null ||
            !projections[0].From.Keys.Concat(projections[0].RemovedWith.Keys).Select(_ => _.Id).ToHashSet()
                .SetEquals(types.Select(_ => _.Id.Value)) ||
            projections[0].From.Values.Any(_ => !string.IsNullOrEmpty(_.Key) && _.Key != "$eventSourceId") ||
            projections[0].RemovedWith.Values.Any(_ => !string.IsNullOrEmpty(_.Key) && _.Key != "$eventSourceId"))
        {
            throw new DecisionReadRefused(DecisionReadRefusalReason.DefinitionMismatch, type);
        }
    }
}
