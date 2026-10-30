// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using FailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_events;

public class and_a_bulk_flush_fails_an_earlier_event_in_the_delivered_partition : given.a_subscriber_receiving_bulk_results
{
    static readonly Key _otherPartition = (Key)"another-partition";
    AppendedEvent _secondEvent;
    AppendedEvent _thirdEvent;
    int _runs;

    void Establish()
    {
        _secondEvent = _event with { Context = _event.Context with { SequenceNumber = 2UL } };
        _thirdEvent = _event with { Context = _event.Context with { SequenceNumber = 3UL } };
        _cachedPipeline.Handle(Arg.Any<AppendedEvent>()).Returns(call =>
        {
            var context = ContextFor(call.Arg<AppendedEvent>());
            _runs++;
            if (_runs == 3)
            {
                context.AddFailedPartition(new FailedPartition(_partition, _secondEvent.Context.SequenceNumber));
                context.AddFailedPartition(new FailedPartition(_otherPartition, 7UL));
            }
            return context;
        });
    }

    async Task Because() => _result = await _subscriber.OnNext(_partition, [_event, _secondEvent, _thirdEvent], new(_thirdEvent.Context.SequenceNumber));

    [Fact] void should_report_failure() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_only_count_events_before_the_first_failed_write() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
    [Fact] async Task should_still_register_the_other_partitions_failure() => await _observer.Received(1).PartitionFailed(_otherPartition, (EventSequenceNumber)7UL, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
}
