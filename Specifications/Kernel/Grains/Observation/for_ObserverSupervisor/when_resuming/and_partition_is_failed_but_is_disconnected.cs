// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_resuming;

public class and_partition_is_failed_but_is_disconnected : given.an_observer_and_two_event_types
{
    const string partition = "ca517fc7-6a93-4878-9ccd-8e5b94b264d5";
    async Task Establish()
    {
        state.FailPartition(partition, 0, Array.Empty<string>(), string.Empty);
        await observer.Unsubscribe();
    }

    async Task Because() => await observer.TryResumePartition(partition);

    [Fact] void should_not_subscribe_to_sequences_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Never);
}
