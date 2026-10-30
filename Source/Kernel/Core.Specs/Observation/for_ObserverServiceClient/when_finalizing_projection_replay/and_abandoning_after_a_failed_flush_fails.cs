// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverServiceClient.when_finalizing_projection_replay;

public class and_abandoning_after_a_failed_flush_fails : Specification
{
    IObserverService _cleanSilo;
    IObserverService _failingSilo;
    ObserverDetails _observerDetails;
    Exception _exception;

    void Establish()
    {
        _observerDetails = new(new("observer", "event-store", "namespace", "event-log"), ObserverType.Projection);
        _cleanSilo = Substitute.For<IObserverService>();
        _cleanSilo.FlushReplayFor(_observerDetails).Returns(true);
        _failingSilo = Substitute.For<IObserverService>();
        _failingSilo.FlushReplayFor(_observerDetails).Returns(false);
        _failingSilo.AbandonReplayFor(_observerDetails).Returns(Task.FromException(new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.Unknown)));
    }

    async Task Because() => _exception = await Catch.Exception(() => ObserverServiceClient.FinalizeProjectionReplay([_cleanSilo, _failingSilo], _observerDetails));

    [Fact] void should_report_that_the_replay_was_abandoned() => _exception.ShouldBeOfExactType<ReplayAbandoned>();
    [Fact] void should_carry_the_reason_the_abandon_failed() => _exception.InnerException.ShouldNotBeNull();
    [Fact] void should_not_finalize_the_replay_on_the_clean_silo() => _cleanSilo.DidNotReceive().TryFinalizeReplayFor(_observerDetails);
    [Fact] void should_not_finalize_the_replay_on_the_failing_silo() => _failingSilo.DidNotReceive().TryFinalizeReplayFor(_observerDetails);
    [Fact] void should_still_abandon_the_replay_on_the_clean_silo() => _cleanSilo.Received(1).AbandonReplayFor(_observerDetails);
}
