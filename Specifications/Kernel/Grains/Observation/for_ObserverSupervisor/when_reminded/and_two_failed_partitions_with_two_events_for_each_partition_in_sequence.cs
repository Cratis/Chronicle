// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.EventSequences;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_reminded;

public class and_two_failed_partitions_with_two_events_for_each_partition_in_sequence : given.an_observer_with_event_types_a_reminder_and_two_failing_partitions
{
    AppendedEvent first_partition_appended_event;
    AppendedEvent second_partition_appended_event;

    Dictionary<EventSourceId, AppendedEvent> events_received;

    void Establish()
    {
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult((EventSequenceNumber)83));

        first_partition_appended_event = new AppendedEvent(
            new(first_partition_failed_sequence, event_types.ToArray()[0]),
            new(first_partition, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new ExpandoObject());

        second_partition_appended_event = new AppendedEvent(
            new(second_partition_failed_sequence, event_types.ToArray()[0]),
            new(second_partition, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new ExpandoObject());

        events_received = new();
        subscriber.Setup(_ => _.OnNext(IsAny<AppendedEvent>()))
            .Returns((AppendedEvent @event) =>
            {
                events_received[@event.Context.EventSourceId] = @event;
                return Task.FromResult(ObserverSubscriberResult.Ok);
            });

        var subscription = new Mock<StreamSubscriptionHandle<AppendedEvent>>();
        sequence_stream.Setup(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()))
            .Returns((IAsyncObserver<AppendedEvent> observer, StreamSequenceToken token, StreamFilterPredicate __, object ___) =>
            {
                subscribed_token = token as EventSequenceNumberToken;
                subscribed_tokens.Add(subscribed_token);

                return Task.FromResult(subscription.Object);
            });
    }

    async Task Because()
    {
        await observer.Subscribe<ObserverSubscriber>(event_types);
        await observer.ReceiveReminder(ObserverSupervisor.RecoverReminder, new TickStatus());
    }

    [Fact] void should_forward_first_partition_event_to_first_partition_observer_subscriber() => events_received[first_partition].ShouldEqual(first_partition_appended_event);
    [Fact] void should_forward_second_partition_event_to_second_partition_observer_subscriber() => events_received[second_partition].ShouldEqual(second_partition_appended_event);
}
