// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_reminded;

public class and_two_failed_partitions_that_has_started_recovering : given.an_observer_with_event_types_a_reminder_and_two_failing_partitions
{
    void Establish()
    {
        state.StartRecoveringPartition(first_partition);
        state.StartRecoveringPartition(second_partition);
    }

    async Task Because() => await observer.ReceiveReminder(Observer.RecoverReminder, new TickStatus());

    [Fact] void should_start_recovering_first_partition() => state.IsRecoveringPartition(first_partition);
    [Fact] void should_start_recovering_second_partition() => state.IsRecoveringPartition(second_partition);
    [Fact] void should_not_subscribe_to_sequences_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Never());
}
