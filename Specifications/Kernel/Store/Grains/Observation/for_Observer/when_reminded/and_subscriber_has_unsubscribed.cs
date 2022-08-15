// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_reminded;

public class and_subscriber_has_unsubscribed : given.an_observer_with_event_types_and_reminder
{
    const string first_partition = "0fbbbd40-1380-4a9d-bbbe-7b46b324f537";
    const string second_partition = "db5fa43e-32eb-4481-9e74-d13b7a501ab3";

    async Task Establish()
    {
        state.FailPartition(first_partition, 42, Array.Empty<string>(), string.Empty);
        state.FailPartition(second_partition, 43, Array.Empty<string>(), string.Empty);
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult((EventSequenceNumber)83));
        await observer.Unsubscribe();
    }

    async Task Because() => await observer.ReceiveReminder(Observer.RecoverReminder, new TickStatus());

    [Fact] void should_not_start_recovering_first_partition() => state.IsRecoveringPartition(first_partition).ShouldBeFalse();
    [Fact] void should_not_start_recovering_second_partition() => state.IsRecoveringPartition(second_partition).ShouldBeFalse();
    [Fact] void should_not_subscribe_to_sequences_stream_for_any_partitions() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Never());
}
