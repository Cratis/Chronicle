// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_subscriber_defers_before_handling_any_events : given.a_performing_job_step
{
    PartitionDeferral _deferral;
    Catch<JobStepResult> _result;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now, now.AddMinutes(1), first_event_sequence_number);
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(new ObserverSubscriberResult(ObserverSubscriberState.Deferred, EventSequenceNumber.Unavailable, [], string.Empty) { Deferral = _deferral });
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_persist_the_deferral() => _observer.Received(1).PartitionDeferred("some-partition", _deferral, null, null);
    [Fact] void should_stop_without_claiming_completion() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_not_record_any_progress() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.IsActualValue.ShouldBeFalse();
    [Fact] void should_not_count_any_events() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents)).ShouldEqual(0);
}
