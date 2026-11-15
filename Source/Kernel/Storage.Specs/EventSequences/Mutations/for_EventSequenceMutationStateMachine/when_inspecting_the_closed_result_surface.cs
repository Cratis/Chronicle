// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_inspecting_the_closed_result_surface : Specification
{
    [Fact] void should_reserve_default_transition_outcome_as_unknown() => default(EventSequenceMutationTransitionOutcome).ShouldEqual(EventSequenceMutationTransitionOutcome.Unknown);
    [Fact] void should_reserve_default_archive_outcome_as_unknown() => default(EventSequenceMutationArchiveOutcome).ShouldEqual(EventSequenceMutationArchiveOutcome.Unknown);
    [Fact] void should_not_expose_transition_result_construction() => typeof(EventSequenceMutationTransitionResult).GetConstructors().ShouldBeEmpty();
    [Fact] void should_not_expose_archive_result_construction() => typeof(EventSequenceMutationArchiveResult).GetConstructors().ShouldBeEmpty();
    [Fact] void should_not_expose_validation_result_construction() => typeof(EventSequenceMutationValidationResult).GetConstructors().ShouldBeEmpty();
    [Fact] void should_require_the_explicit_valid_validation_instance() => EventSequenceMutationValidationResult.Valid.IsValid.ShouldBeTrue();
}
