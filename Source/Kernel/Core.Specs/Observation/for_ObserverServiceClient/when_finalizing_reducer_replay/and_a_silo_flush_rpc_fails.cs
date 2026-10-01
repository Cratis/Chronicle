// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverServiceClient.when_finalizing_reducer_replay;

public class and_a_silo_flush_rpc_fails : Specification
{
    IObserverService _unreachable;
    IObserverService _surviving;
    ObserverDetails _details;
    Exception _failure;
    Exception _error;

    void Establish()
    {
        _details = new(new("reducer", "store", "namespace", "event-log"), ObserverType.Reducer);
        _unreachable = Substitute.For<IObserverService>();
        _surviving = Substitute.For<IObserverService>();
        _failure = new InvalidOperationException("Silo disappeared");
        _unreachable.FlushReplayFor(_details).Returns(Task.FromException<bool>(_failure));
        _unreachable.TryFinalizeReplayFor(Arg.Any<ObserverDetails>()).Returns(Task.FromException<bool>(new InvalidOperationException("Still unreachable")));
        _surviving.FlushReplayFor(_details).Returns(true);
        _surviving.TryFinalizeReplayFor(Arg.Any<ObserverDetails>()).Returns(true);
    }

    async Task Because() => _error = await Catch.Exception(() => ObserverServiceClient.FinalizeProjectionReplay([_unreachable, _surviving], _details));

    [Fact] void should_preserve_the_barrier_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_clean_up_the_surviving_silo() => _surviving.Received(1).TryFinalizeReplayFor(Arg.Is<ObserverDetails>(details => details.ReplayAborted));
    [Fact] void should_attempt_cleanup_on_every_silo() => _unreachable.Received(1).TryFinalizeReplayFor(Arg.Is<ObserverDetails>(details => details.ReplayAborted));
    [Fact] void should_not_promote_any_silo() => _surviving.DidNotReceive().TryFinalizeReplayFor(Arg.Is<ObserverDetails>(details => !details.ReplayAborted));
}
