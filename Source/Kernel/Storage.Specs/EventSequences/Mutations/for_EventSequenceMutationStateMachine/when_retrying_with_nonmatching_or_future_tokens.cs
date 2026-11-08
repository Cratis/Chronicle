// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_retrying_with_nonmatching_or_future_tokens : given.a_valid_mutation_state
{
    EventSequenceMutationTransitionResult[] _nonmatching;
    EventSequenceMutationTransitionResult _future;
    EventSequenceMutationTransitionResult _maximumExactRetry;

    void Because()
    {
        var legal = LegalTransitions();
        _nonmatching = legal
            .SelectMany(edge => Enum.GetValues<EventSequenceMutationTransition>()
                .Where(transition => transition != EventSequenceMutationTransition.Unspecified && transition != edge.Transition)
                .Select(transition => EventSequenceMutationStateMachine.Apply(_scope, edge.Successor, transition, Token(edge.Source))))
            .ToArray();

        var futureTokenState = _active with { StateVersion = _active.StateVersion.Value + 1 };
        _future = EventSequenceMutationStateMachine.Apply(
            _scope,
            _active,
            EventSequenceMutationTransition.BeginApplying,
            UncheckedToken(_scope, futureTokenState));

        var boundarySource = Mutation(EventSequenceMutationPhase.Reserved, stateVersion: long.MaxValue - 1);
        var boundaryToken = Token(boundarySource);
        var boundaryApplied = EventSequenceMutationStateMachine.Apply(
            _scope,
            boundarySource,
            EventSequenceMutationTransition.BeginApplying,
            boundaryToken);
        _maximumExactRetry = EventSequenceMutationStateMachine.Apply(
            _scope,
            boundaryApplied.Mutation!,
            EventSequenceMutationTransition.BeginApplying,
            boundaryToken);
    }

    [Fact] void should_reject_every_nonmatching_one_version_retry() => _nonmatching.All(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_not_return_tokens_for_nonmatching_retries() => _nonmatching.All(_ => _.Token is null).ShouldBeTrue();
    [Fact] void should_reject_a_token_ahead_of_current_state() => _future.Outcome.ShouldEqual(EventSequenceMutationTransitionOutcome.Conflict);
    [Fact] void should_recognize_an_exact_retry_at_the_maximum_version_boundary() => (_maximumExactRetry.Outcome == EventSequenceMutationTransitionOutcome.AlreadyApplied && _maximumExactRetry.Mutation!.StateVersion.Value == long.MaxValue).ShouldBeTrue();
}
