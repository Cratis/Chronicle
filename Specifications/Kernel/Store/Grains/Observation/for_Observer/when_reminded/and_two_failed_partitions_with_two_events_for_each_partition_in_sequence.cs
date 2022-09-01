// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store.EventSequences;
using Aksio.Cratis.Execution;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_reminded;

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
            new(first_partition, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new JsonObject());

        second_partition_appended_event = new AppendedEvent(
            new(second_partition_failed_sequence, event_types.ToArray()[0]),
            new(second_partition, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new JsonObject());

        events_received = new();
        observer_stream.Setup(_ => _.OnNextAsync(IsAny<AppendedEvent>(), IsAny<StreamSequenceToken>()))
            .Returns((AppendedEvent @event, StreamSequenceToken _) =>
            {
                events_received[@event.Context.EventSourceId] = @event;
                return Task.CompletedTask;
            });

        var subscription = new Mock<StreamSubscriptionHandle<AppendedEvent>>();
        sequence_stream.Setup(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()))
            .Returns((IAsyncObserver<AppendedEvent> observer, StreamSequenceToken token, StreamFilterPredicate __, object ___) =>
            {
                subscribed_token = token as EventSequenceNumberTokenWithFilter;
                subscribed_tokens.Add(subscribed_token);

                if (subscribed_token.Partition == first_partition)
                {
                    observer.OnNextAsync(first_partition_appended_event, subscribed_token);
                }

                if (subscribed_token.Partition == second_partition)
                {
                    observer.OnNextAsync(second_partition_appended_event, subscribed_token);
                }

                return Task.FromResult(subscription.Object);
            });
    }

    async Task Because() => await observer.ReceiveReminder(Observer.RecoverReminder, new TickStatus());

    [Fact] void should_forward_first_partition_event_to_first_partition_observer_stream() => events_received[first_partition].ShouldEqual(first_partition_appended_event);
    [Fact] void should_forward_second_partition_event_to_second_partition_observer_stream() => events_received[second_partition].ShouldEqual(second_partition_appended_event);
}
