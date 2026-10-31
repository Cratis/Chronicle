// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverServiceClient.when_finalizing_projection_replay;

/// <summary>
/// A flush that could not complete at all is no more a clean flush than one that left failed partitions.
/// </summary>
public class and_a_silo_flush_faults : Specification
{
    IObserverService _cleanSilo;
    IObserverService _faultingSilo;
    ObserverDetails _observerDetails;
    Exception _flushError;
    Exception _exception;

    void Establish()
    {
        _observerDetails = new(new("observer", "event-store", "namespace", "event-log"), ObserverType.Projection);
        _flushError = new TimeoutException("The silo did not answer");
        _cleanSilo = Substitute.For<IObserverService>();
        _cleanSilo.FlushReplayFor(_observerDetails).Returns(true);
        _faultingSilo = Substitute.For<IObserverService>();
        _faultingSilo.FlushReplayFor(_observerDetails).Returns(Task.FromException<bool>(_flushError));
    }

    async Task Because() => _exception = await Catch.Exception(() => ObserverServiceClient.FinalizeProjectionReplay([_cleanSilo, _faultingSilo], _observerDetails));

    [Fact] void should_report_that_the_replay_was_abandoned() => _exception.ShouldBeOfExactType<ReplayAbandoned>();
    [Fact] void should_carry_the_flush_error() => _exception.InnerException.ShouldEqual(_flushError);
    [Fact] void should_not_finalize_the_replay_on_the_clean_silo() => _cleanSilo.DidNotReceive().TryFinalizeReplayFor(_observerDetails);
    [Fact] void should_not_finalize_the_replay_on_the_faulting_silo() => _faultingSilo.DidNotReceive().TryFinalizeReplayFor(_observerDetails);
    [Fact] void should_abandon_the_replay_on_the_clean_silo() => _cleanSilo.Received(1).AbandonReplayFor(_observerDetails);
    [Fact] void should_abandon_the_replay_on_the_faulting_silo() => _faultingSilo.Received(1).AbandonReplayFor(_observerDetails);
}
