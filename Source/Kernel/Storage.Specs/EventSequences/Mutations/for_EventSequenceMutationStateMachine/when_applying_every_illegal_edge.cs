// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_applying_every_illegal_edge : given.a_valid_mutation_state
{
    (EventSequenceMutation Current, EventSequenceMutationTransitionResult Result)[] _results;

    void Because()
    {
        var legal = LegalTransitions();
        _results = ValidStates()
            .SelectMany(current => Enum.GetValues<EventSequenceMutationTransition>()
                .Where(transition =>
                    transition != EventSequenceMutationTransition.Unspecified &&
                    !legal.Any(_ => _.Source == current && _.Transition == transition))
                .Select(transition => (current, EventSequenceMutationStateMachine.Apply(_scope, current, transition, Token(current)))))
            .ToArray();
    }

    [Fact] void should_cover_every_edge_outside_the_closed_graph() => _results.Length.ShouldEqual(79);
    [Fact] void should_report_every_illegal_edge_as_a_conflict() => _results.All(_ => _.Result.Outcome == EventSequenceMutationTransitionOutcome.Conflict).ShouldBeTrue();
    [Fact] void should_return_each_unchanged_current_state() => _results.All(_ => ReferenceEquals(_.Result.Mutation, _.Current)).ShouldBeTrue();
    [Fact] void should_not_return_a_successor_token() => _results.All(_ => _.Result.Token is null).ShouldBeTrue();
    [Fact] void should_not_report_valid_inputs_as_malformed() => _results.All(_ => _.Result.Validation.IsValid).ShouldBeTrue();
}
