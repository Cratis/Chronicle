// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverServiceClient.when_finalizing_projection_replay;

public class and_a_silo_flushed_with_failed_partitions : Specification
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
        _cleanSilo.TryFinalizeReplayFor(_observerDetails).Returns(true);
        _failingSilo = Substitute.For<IObserverService>();
        _failingSilo.FlushReplayFor(_observerDetails).Returns(false);
        _failingSilo.TryFinalizeReplayFor(_observerDetails).Returns(false);
    }

    async Task Because() => _exception = await Catch.Exception(() => ObserverServiceClient.FinalizeProjectionReplay([_cleanSilo, _failingSilo], _observerDetails));

    [Fact] void should_still_finalize_the_replay_on_the_clean_silo() => _cleanSilo.Received(1).TryFinalizeReplayFor(_observerDetails);
    [Fact] void should_still_finalize_the_replay_on_the_failing_silo() => _failingSilo.Received(1).TryFinalizeReplayFor(_observerDetails);

    [Fact] void should_report_that_the_replay_did_not_finalize_cleanly() => _exception.ShouldBeOfExactType<ReplayFinalizationFailed>();
}
