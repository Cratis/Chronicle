// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_retrying_every_exact_transition : given.a_valid_mutation_state
{
    (EventSequenceMutation Current, EventSequenceMutationTransitionResult Result)[] _results;

    void Because() => _results = LegalTransitions()
        .Select(_ => (_.Successor, EventSequenceMutationStateMachine.Apply(_scope, _.Successor, _.Transition, Token(_.Source))))
        .ToArray();

    [Fact] void should_cover_every_legal_edge() => _results.Length.ShouldEqual(11);
    [Fact] void should_report_every_edge_as_already_applied() => _results.All(_ => _.Result.Outcome == EventSequenceMutationTransitionOutcome.AlreadyApplied).ShouldBeTrue();
    [Fact] void should_not_increment_any_state_version_again() => _results.All(_ => _.Result.Mutation!.StateVersion == _.Current.StateVersion).ShouldBeTrue();
    [Fact] void should_return_the_observed_successor() => _results.All(_ => ReferenceEquals(_.Result.Mutation, _.Current)).ShouldBeTrue();
    [Fact] void should_return_each_observed_successor_token() => _results.All(_ => _.Result.Token == EventSequenceMutationStateToken.Create(_scope, _.Current)).ShouldBeTrue();
    [Fact] void should_return_valid_results() => _results.All(_ => _.Result.Validation.IsValid).ShouldBeTrue();
}
