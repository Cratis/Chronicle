// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using FailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_events;

public class and_a_bulk_flush_fails_another_partition : given.a_subscriber_receiving_bulk_results
{
    static readonly Key _otherPartition = (Key)"another-partition";

    void Establish() => _cachedPipeline.Handle(Arg.Any<AppendedEvent>()).Returns(call =>
    {
        var context = ContextFor(call.Arg<AppendedEvent>());
        context.AddFailedPartition(new FailedPartition(_otherPartition, 7UL));
        return context;
    });

    async Task Because() => _result = await _subscriber.OnNext(_partition, [_event], new(_event.Context.SequenceNumber));

    [Fact] void should_keep_the_delivered_partition_successful() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_advance_the_last_handled_event() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
    [Fact] async Task should_register_the_other_partitions_failure() => await _observer.Received(1).PartitionFailed(_otherPartition, (EventSequenceNumber)7UL, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
}
