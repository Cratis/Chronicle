// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_IEventSequenceMutationRegistry;

public class when_inspecting_outcomes : Specification
{
    [Fact]
    void should_pin_begin_outcome_values()
    {
        ShouldHaveValue(EventSequenceMutationBeginOutcome.Unknown, 0);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.Reserved, 1);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.Resumed, 2);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.RecoveredReservation, 3);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.Archived, 4);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.MutationAlreadyInProgress, 5);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.DefinitionConflict, 6);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.Contended, 7);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.Indeterminate, 8);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.Invalid, 9);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.Corrupt, 10);
        ShouldHaveValue(EventSequenceMutationBeginOutcome.Unsupported, 11);
    }

    [Fact]
    void should_pin_transition_outcome_values()
    {
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.Unknown, 0);
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.Applied, 1);
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.AlreadyApplied, 2);
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.AlreadyArchived, 3);
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.StateConflict, 4);
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.Contended, 5);
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.Indeterminate, 6);
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.Invalid, 7);
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.Corrupt, 8);
        ShouldHaveValue(EventSequenceMutationRegistryTransitionOutcome.Unsupported, 9);
    }

    [Fact]
    void should_pin_archive_outcome_values()
    {
        ShouldHaveValue(EventSequenceMutationRegistryArchiveOutcome.Unknown, 0);
        ShouldHaveValue(EventSequenceMutationRegistryArchiveOutcome.Archived, 1);
        ShouldHaveValue(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived, 2);
        ShouldHaveValue(EventSequenceMutationRegistryArchiveOutcome.StateConflict, 3);
        ShouldHaveValue(EventSequenceMutationRegistryArchiveOutcome.Contended, 4);
        ShouldHaveValue(EventSequenceMutationRegistryArchiveOutcome.Indeterminate, 5);
        ShouldHaveValue(EventSequenceMutationRegistryArchiveOutcome.Invalid, 6);
        ShouldHaveValue(EventSequenceMutationRegistryArchiveOutcome.Corrupt, 7);
        ShouldHaveValue(EventSequenceMutationRegistryArchiveOutcome.Unsupported, 8);
    }

    [Fact]
    void should_pin_tracking_outcome_values()
    {
        ShouldHaveValue(EventSequenceMutationTrackingOutcome.Unknown, 0);
        ShouldHaveValue(EventSequenceMutationTrackingOutcome.Began, 1);
        ShouldHaveValue(EventSequenceMutationTrackingOutcome.AlreadyTracking, 2);
        ShouldHaveValue(EventSequenceMutationTrackingOutcome.Conflict, 3);
        ShouldHaveValue(EventSequenceMutationTrackingOutcome.Contended, 4);
        ShouldHaveValue(EventSequenceMutationTrackingOutcome.Indeterminate, 5);
        ShouldHaveValue(EventSequenceMutationTrackingOutcome.Invalid, 6);
        ShouldHaveValue(EventSequenceMutationTrackingOutcome.Corrupt, 7);
        ShouldHaveValue(EventSequenceMutationTrackingOutcome.Unsupported, 8);
    }

    [Fact] void should_reserve_default_begin_outcome_as_unknown() => default(EventSequenceMutationBeginOutcome).ShouldEqual(EventSequenceMutationBeginOutcome.Unknown);
    [Fact] void should_reserve_default_transition_outcome_as_unknown() => default(EventSequenceMutationRegistryTransitionOutcome).ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.Unknown);
    [Fact] void should_reserve_default_archive_outcome_as_unknown() => default(EventSequenceMutationRegistryArchiveOutcome).ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Unknown);
    [Fact] void should_reserve_default_tracking_outcome_as_unknown() => default(EventSequenceMutationTrackingOutcome).ShouldEqual(EventSequenceMutationTrackingOutcome.Unknown);
    [Fact] void should_reserve_default_registry_error_as_unknown() => default(EventSequenceMutationRegistryError).ShouldEqual(EventSequenceMutationRegistryError.Unknown);

    static void ShouldHaveValue<TEnum>(TEnum value, int expected)
        where TEnum : struct, Enum => Convert.ToInt32(value).ShouldEqual(expected);
}
