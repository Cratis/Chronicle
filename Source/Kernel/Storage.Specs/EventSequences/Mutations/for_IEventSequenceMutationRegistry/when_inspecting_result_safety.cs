// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_IEventSequenceMutationRegistry;

public class when_inspecting_result_safety : given.a_registry_contract
{
    static readonly Type[] _resultTypes =
    [
        typeof(EventSequenceMutationBeginResult),
        typeof(EventSequenceMutationRegistryTransitionResult),
        typeof(EventSequenceMutationRegistryArchiveResult),
        typeof(EventSequenceMutationTrackingResult)
    ];

    static readonly Type[] _sensitiveTypes =
    [
        typeof(EventSequenceMutationHead),
        typeof(EventSequenceMutationRegistration),
        typeof(EventSequenceMutation),
        typeof(EventSequenceMutationDefinition),
        typeof(EventSequenceMutationRequest),
        typeof(EventSequenceMutationCommandEnvelope)
    ];

    [Fact] void should_not_expose_public_result_constructors() => _resultTypes.All(_ => _.GetConstructors().Length == 0).ShouldBeTrue();
    [Fact]
    void should_not_expose_command_or_payload_properties_on_results() =>
        _resultTypes.SelectMany(_ => _.GetProperties()).Any(_ =>
            string.Equals(_.Name, "Command", StringComparison.Ordinal) ||
            string.Equals(_.Name, "Payload", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_expose_registration_from_any_archived_result_type() =>
        new[]
        {
            typeof(EventSequenceMutationBeginResult),
            typeof(EventSequenceMutationRegistryTransitionResult),
            typeof(EventSequenceMutationRegistryArchiveResult)
        }.Any(_ => _.GetProperty("Registration") is not null).ShouldBeFalse();
    [Fact] void should_not_expose_a_head_or_observed_head_from_tracking_results() =>
        new[] { "Head", "ObservedHead" }.Any(_ => typeof(EventSequenceMutationTrackingResult).GetProperty(_) is not null).ShouldBeFalse();
    [Fact] void should_fail_closed_for_uninitialized_begin_results() => Uninitialized<EventSequenceMutationBeginResult>().IsSuccess.ShouldBeFalse();
    [Fact] void should_fail_closed_for_uninitialized_transition_results() => Uninitialized<EventSequenceMutationRegistryTransitionResult>().IsSuccess.ShouldBeFalse();
    [Fact] void should_fail_closed_for_uninitialized_archive_results() => Uninitialized<EventSequenceMutationRegistryArchiveResult>().IsSuccess.ShouldBeFalse();
    [Fact] void should_fail_closed_for_uninitialized_tracking_results() => Uninitialized<EventSequenceMutationTrackingResult>().IsSuccess.ShouldBeFalse();

    [Fact]
    void should_not_expose_a_path_to_command_payload_from_any_failure_or_conflict_result_graph()
    {
        var results = new object[]
        {
            EventSequenceMutationBeginResult.MutationAlreadyInProgress(_request.Id),
            EventSequenceMutationBeginResult.DefinitionConflict(_request.Id),
            EventSequenceMutationBeginResult.Contended(),
            EventSequenceMutationBeginResult.Indeterminate(),
            EventSequenceMutationBeginResult.Invalid(_invalidValidation),
            EventSequenceMutationBeginResult.Corrupt(),
            EventSequenceMutationBeginResult.Unsupported(),
            EventSequenceMutationRegistryTransitionResult.StateConflict(),
            EventSequenceMutationRegistryTransitionResult.Contended(),
            EventSequenceMutationRegistryTransitionResult.Indeterminate(),
            EventSequenceMutationRegistryTransitionResult.Invalid(_invalidValidation),
            EventSequenceMutationRegistryTransitionResult.Corrupt(),
            EventSequenceMutationRegistryTransitionResult.Unsupported(),
            EventSequenceMutationRegistryArchiveResult.StateConflict(),
            EventSequenceMutationRegistryArchiveResult.Contended(),
            EventSequenceMutationRegistryArchiveResult.Indeterminate(),
            EventSequenceMutationRegistryArchiveResult.Invalid(_invalidValidation),
            EventSequenceMutationRegistryArchiveResult.Corrupt(),
            EventSequenceMutationRegistryArchiveResult.Unsupported(),
            EventSequenceMutationTrackingResult.Conflict(EventSequenceMutationCoverage.Sealed),
            EventSequenceMutationTrackingResult.Contended(),
            EventSequenceMutationTrackingResult.Indeterminate(),
            EventSequenceMutationTrackingResult.Invalid(_invalidValidation),
            EventSequenceMutationTrackingResult.Corrupt(),
            EventSequenceMutationTrackingResult.Unsupported()
        };

        results.All(HasNoSensitivePublicPath).ShouldBeTrue();
    }

    [Fact]
    void should_not_expose_registration_or_sensitive_state_from_any_archived_result_graph()
    {
        var results = new object[]
        {
            EventSequenceMutationBeginResult.Archived(_scope, _archivedRegistration, _history),
            EventSequenceMutationRegistryTransitionResult.AlreadyArchived(_scope, _archivedRegistration, _history),
            EventSequenceMutationRegistryArchiveResult.Archived(_scope, _archivedRegistration, _history),
            EventSequenceMutationRegistryArchiveResult.AlreadyArchived(_scope, _archivedRegistration, _history)
        };

        results.All(HasNoSensitivePublicPath).ShouldBeTrue();
        results.Any(_ => _.GetType().GetProperty("Registration") is not null).ShouldBeFalse();
    }

    [Fact]
    void should_only_populate_active_on_active_success_results()
    {
        var activeResults = new object[]
        {
            EventSequenceMutationBeginResult.Reserved(_active, _token),
            EventSequenceMutationBeginResult.Resumed(_active, _token),
            EventSequenceMutationBeginResult.RecoveredReservation(_active, _token),
            EventSequenceMutationRegistryTransitionResult.Applied(_active, _token),
            EventSequenceMutationRegistryTransitionResult.AlreadyApplied(_active, _token)
        };
        var nonActiveResults = new object[]
        {
            EventSequenceMutationBeginResult.Archived(_scope, _archivedRegistration, _history),
            EventSequenceMutationBeginResult.MutationAlreadyInProgress(_request.Id),
            EventSequenceMutationRegistryTransitionResult.AlreadyArchived(_scope, _archivedRegistration, _history),
            EventSequenceMutationRegistryTransitionResult.StateConflict()
        };

        activeResults.All(_ => ReferenceEquals(_.GetType().GetProperty("Active")!.GetValue(_), _active)).ShouldBeTrue();
        nonActiveResults.All(_ => _.GetType().GetProperty("Active")?.GetValue(_) is null).ShouldBeTrue();
    }

    [Fact]
    void should_not_render_command_payload_from_any_result_or_error()
    {
        const string sensitiveFragment = "privateCommand";
        var values = new object[]
        {
            EventSequenceMutationBeginResult.Reserved(_active, _token),
            EventSequenceMutationBeginResult.Archived(_scope, _archivedRegistration, _history),
            EventSequenceMutationBeginResult.MutationAlreadyInProgress(_request.Id),
            EventSequenceMutationBeginResult.DefinitionConflict(_request.Id),
            EventSequenceMutationRegistryTransitionResult.Applied(_active, _token),
            EventSequenceMutationRegistryTransitionResult.AlreadyArchived(_scope, _archivedRegistration, _history),
            EventSequenceMutationRegistryTransitionResult.StateConflict(),
            EventSequenceMutationRegistryArchiveResult.Archived(_scope, _archivedRegistration, _history),
            EventSequenceMutationRegistryArchiveResult.StateConflict(),
            EventSequenceMutationTrackingResult.Began(),
            EventSequenceMutationTrackingResult.Conflict(EventSequenceMutationCoverage.Sealed),
            EventSequenceMutationRegistryError.DefinitionConflict
        };

        values.Any(_ => _.ToString()!.Contains(sensitiveFragment, StringComparison.Ordinal)).ShouldBeFalse();
    }

    static bool HasNoSensitivePublicPath(object root)
    {
        var pending = new Stack<object>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        pending.Push(root);

        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var property in current.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(_ => _.GetIndexParameters().Length == 0))
            {
                var value = property.GetValue(current);
                if (value is null)
                {
                    continue;
                }

                var declaredType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var runtimeType = value.GetType();
                if (_sensitiveTypes.Contains(declaredType) || _sensitiveTypes.Contains(runtimeType) ||
                    string.Equals(property.Name, "Payload", StringComparison.Ordinal))
                {
                    return false;
                }

                if (!runtimeType.IsValueType && runtimeType != typeof(string) && runtimeType.Namespace?.StartsWith("System", StringComparison.Ordinal) != true)
                {
                    pending.Push(value);
                }
            }
        }

        return true;
    }

    static T Uninitialized<T>()
        where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
}
