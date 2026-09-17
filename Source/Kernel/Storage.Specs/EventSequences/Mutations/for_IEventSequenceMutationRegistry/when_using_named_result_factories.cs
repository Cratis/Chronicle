// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_IEventSequenceMutationRegistry;

public class when_using_named_result_factories : given.a_registry_contract
{
    [Fact]
    void should_create_every_active_begin_success_with_only_active_payload()
    {
        var results = new[]
        {
            EventSequenceMutationBeginResult.Reserved(_active, _token),
            EventSequenceMutationBeginResult.Resumed(_active, _token),
            EventSequenceMutationBeginResult.RecoveredReservation(_active, _token)
        };

        results.All(_ => _.IsSuccess && ReferenceEquals(_.Active, _active) && ReferenceEquals(_.Token, _token) &&
            _.History is null && _.ConflictingMutationId is null &&
            _.Error == EventSequenceMutationRegistryError.Unknown && _.Validation is null).ShouldBeTrue();
    }

    [Fact]
    void should_create_begin_archived_with_only_payload_free_history()
    {
        var result = EventSequenceMutationBeginResult.Archived(_scope, _archivedRegistration, _history);

        result.IsSuccess.ShouldBeTrue();
        result.History.ShouldEqual(_history);
        result.Active.ShouldBeNull();
        result.Token.ShouldBeNull();
        result.ConflictingMutationId.ShouldBeNull();
    }

    [Fact]
    void should_create_every_begin_failure_with_only_its_typed_details()
    {
        var results = new[]
        {
            EventSequenceMutationBeginResult.MutationAlreadyInProgress(_request.Id),
            EventSequenceMutationBeginResult.DefinitionConflict(_request.Id),
            EventSequenceMutationBeginResult.Contended(),
            EventSequenceMutationBeginResult.Indeterminate(),
            EventSequenceMutationBeginResult.Invalid(_invalidValidation),
            EventSequenceMutationBeginResult.Corrupt(),
            EventSequenceMutationBeginResult.Unsupported()
        };

        results.All(_ => !_.IsSuccess && _.Active is null && _.Token is null && _.History is null && _.Error != EventSequenceMutationRegistryError.Unknown).ShouldBeTrue();
        results.Single(_ => _.Outcome == EventSequenceMutationBeginOutcome.MutationAlreadyInProgress).ConflictingMutationId.ShouldEqual(_request.Id);
        results.Single(_ => _.Outcome == EventSequenceMutationBeginOutcome.DefinitionConflict).ConflictingMutationId.ShouldEqual(_request.Id);
        results.Single(_ => _.Outcome == EventSequenceMutationBeginOutcome.Invalid).Validation.ShouldEqual(_invalidValidation);
    }

    [Fact]
    void should_create_every_transition_success_with_only_its_success_payload()
    {
        var activeResults = new[]
        {
            EventSequenceMutationRegistryTransitionResult.Applied(_active, _token),
            EventSequenceMutationRegistryTransitionResult.AlreadyApplied(_active, _token)
        };
        var archived = EventSequenceMutationRegistryTransitionResult.AlreadyArchived(_scope, _archivedRegistration, _history);

        activeResults.All(_ => _.IsSuccess && ReferenceEquals(_.Active, _active) && ReferenceEquals(_.Token, _token) && _.History is null).ShouldBeTrue();
        archived.IsSuccess.ShouldBeTrue();
        archived.History.ShouldEqual(_history);
        archived.Active.ShouldBeNull();
        archived.Token.ShouldBeNull();
    }

    [Fact]
    void should_create_every_transition_failure_without_state_payload()
    {
        var results = new[]
        {
            EventSequenceMutationRegistryTransitionResult.StateConflict(),
            EventSequenceMutationRegistryTransitionResult.Contended(),
            EventSequenceMutationRegistryTransitionResult.Indeterminate(),
            EventSequenceMutationRegistryTransitionResult.Invalid(_invalidValidation),
            EventSequenceMutationRegistryTransitionResult.Corrupt(),
            EventSequenceMutationRegistryTransitionResult.Unsupported()
        };

        results.All(_ => !_.IsSuccess && _.Active is null && _.Token is null && _.History is null && _.Error != EventSequenceMutationRegistryError.Unknown).ShouldBeTrue();
        results.Single(_ => _.Outcome == EventSequenceMutationRegistryTransitionOutcome.Invalid).Validation.ShouldEqual(_invalidValidation);
    }

    [Fact]
    void should_create_every_archive_success_with_only_payload_free_history()
    {
        var results = new[]
        {
            EventSequenceMutationRegistryArchiveResult.Archived(_scope, _archivedRegistration, _history),
            EventSequenceMutationRegistryArchiveResult.AlreadyArchived(_scope, _archivedRegistration, _history)
        };

        results.All(_ => _.IsSuccess && ReferenceEquals(_.History, _history) &&
            _.Error == EventSequenceMutationRegistryError.Unknown && _.Validation is null).ShouldBeTrue();
    }

    [Fact]
    void should_create_every_archive_failure_without_state_payload()
    {
        var results = new[]
        {
            EventSequenceMutationRegistryArchiveResult.StateConflict(),
            EventSequenceMutationRegistryArchiveResult.Contended(),
            EventSequenceMutationRegistryArchiveResult.Indeterminate(),
            EventSequenceMutationRegistryArchiveResult.Invalid(_invalidValidation),
            EventSequenceMutationRegistryArchiveResult.Corrupt(),
            EventSequenceMutationRegistryArchiveResult.Unsupported()
        };

        results.All(_ => !_.IsSuccess && _.History is null && _.Error != EventSequenceMutationRegistryError.Unknown).ShouldBeTrue();
        results.Single(_ => _.Outcome == EventSequenceMutationRegistryArchiveOutcome.Invalid).Validation.ShouldEqual(_invalidValidation);
    }

    [Fact]
    void should_create_every_tracking_success_with_only_unsealed_coverage()
    {
        var results = new[]
        {
            EventSequenceMutationTrackingResult.Began(),
            EventSequenceMutationTrackingResult.AlreadyTracking()
        };

        results.All(_ => _.IsSuccess && _.Coverage == EventSequenceMutationCoverage.Unsealed &&
            _.Error == EventSequenceMutationRegistryError.Unknown && _.Validation is null).ShouldBeTrue();
    }

    [Fact]
    void should_create_every_tracking_failure_with_only_its_typed_details()
    {
        var results = new[]
        {
            EventSequenceMutationTrackingResult.Conflict(EventSequenceMutationCoverage.Sealed),
            EventSequenceMutationTrackingResult.Contended(),
            EventSequenceMutationTrackingResult.Indeterminate(),
            EventSequenceMutationTrackingResult.Invalid(_invalidValidation),
            EventSequenceMutationTrackingResult.Corrupt(),
            EventSequenceMutationTrackingResult.Unsupported()
        };

        results.All(_ => !_.IsSuccess && _.Error != EventSequenceMutationRegistryError.Unknown).ShouldBeTrue();
        results.Single(_ => _.Outcome == EventSequenceMutationTrackingOutcome.Conflict).Coverage.ShouldEqual(EventSequenceMutationCoverage.Sealed);
        results.Where(_ => _.Outcome != EventSequenceMutationTrackingOutcome.Conflict).All(_ => _.Coverage is null).ShouldBeTrue();
        results.Single(_ => _.Outcome == EventSequenceMutationTrackingOutcome.Invalid).Validation.ShouldEqual(_invalidValidation);
    }
}
