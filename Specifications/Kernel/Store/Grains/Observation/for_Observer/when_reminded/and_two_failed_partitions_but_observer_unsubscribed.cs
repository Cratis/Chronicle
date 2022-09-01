// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_reminded;

public class and_two_failed_partitions_but_observer_unsubscribed : given.an_observer_with_event_types_a_reminder_and_two_failing_partitions
{
    async Task Establish()
    {
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult((EventSequenceNumber)83));
        await observer.Unsubscribe();
    }

    async Task Because() => await observer.ReceiveReminder(Observer.RecoverReminder, new TickStatus());

    [Fact] void should_not_subscribe_to_sequences_stream_for_any_partitions() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Never());
}
