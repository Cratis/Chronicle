// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using FailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_events;

public class and_a_bulk_flush_fails_the_delivered_partition : given.a_subscriber_receiving_bulk_results
{
    void Establish() => _cachedPipeline.Handle(Arg.Any<AppendedEvent>()).Returns(call =>
    {
        var context = ContextFor(call.Arg<AppendedEvent>());
        context.AddFailedPartition(new FailedPartition(_partition, context.Event.Context.SequenceNumber) { Reason = "write rejected" });
        return context;
    });

    async Task Because() => _result = await _subscriber.OnNext(_partition, [_event], new(_event.Context.SequenceNumber));

    [Fact] void should_report_failure_instead_of_recovering_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_advance_the_last_handled_event() => _result.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_preserve_the_stores_failure_reason() => _result.ExceptionMessages.ShouldContain($"Bulk operation failed for partition {_partition}: write rejected");
    [Fact] async Task should_leave_the_current_failure_for_the_delivery_failure_path_to_register() => await _observer.DidNotReceive().PartitionFailed(_partition, _event.Context.SequenceNumber, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
}
