// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_applying_every_legal_transition : given.a_valid_mutation_state
{
    (EventSequenceMutation Source, EventSequenceMutation Before, EventSequenceMutation Successor, EventSequenceMutationTransitionResult Result)[] _results;

    void Because() => _results = LegalTransitions()
        .Select(_ => (_.Source, _.Source with { }, _.Successor, EventSequenceMutationStateMachine.Apply(_scope, _.Source, _.Transition, Token(_.Source))))
        .ToArray();

    [Fact] void should_cover_the_complete_legal_graph() => _results.Length.ShouldEqual(11);
    [Fact] void should_apply_every_transition() => _results.All(_ => _.Result.Outcome == EventSequenceMutationTransitionOutcome.Applied).ShouldBeTrue();
    [Fact] void should_increment_each_state_version_exactly_once() => _results.All(_ => _.Result.Mutation!.StateVersion.Value == _.Source.StateVersion.Value + 1).ShouldBeTrue();
    [Fact] void should_produce_each_expected_successor() => _results.All(_ => _.Result.Mutation == _.Successor).ShouldBeTrue();
    [Fact] void should_return_each_successor_token() => _results.All(_ => _.Result.Token == EventSequenceMutationStateToken.Create(_scope, _.Successor)).ShouldBeTrue();
    [Fact] void should_return_valid_results() => _results.All(_ => _.Result.Validation.IsValid).ShouldBeTrue();
    [Fact] void should_leave_every_current_state_unchanged() => _results.All(_ => _.Source == _.Before).ShouldBeTrue();
}
