// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_reminded;

public class and_two_failed_partitions_that_has_started_recovering : given.a_connected_observer_with_event_types_and_reminder
{
    const string first_partition = "0fbbbd40-1380-4a9d-bbbe-7b46b324f537";
    const string second_partition = "db5fa43e-32eb-4481-9e74-d13b7a501ab3";

    void Establish()
    {
        state.FailPartition(first_partition, 0, Array.Empty<string>(), string.Empty);
        state.FailPartition(second_partition, 0, Array.Empty<string>(), string.Empty);
        state.StartRecoveringPartition(first_partition);
        state.StartRecoveringPartition(second_partition);
    }

    async Task Because() => await observer.ReceiveReminder(Observer.RecoverReminder, new TickStatus());

    [Fact] void should_start_recovering_first_partition() => state.IsRecoveringPartition(first_partition);
    [Fact] void should_start_recovering_second_partition() => state.IsRecoveringPartition(second_partition);
    [Fact] void should_not_subscribe_to_sequences_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Never());
}
