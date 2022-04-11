// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_resuming;

public class and_partition_is_failed : given.a_connected_observer_and_two_event_types
{
    const string partition = "ca517fc7-6a93-4878-9ccd-8e5b94b264d5";
    void Establish()
    {
        state.FailPartition(partition, 42, Array.Empty<string>(), string.Empty);
    }

    async Task Because() => await observer.TryResumePartition(partition);

    [Fact] void should_not_subscribe_to_sequences_stream() => stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Once());
    [Fact] void should_subscribe_with_offset_at_failed_partition_stopped() => subscribed_token.SequenceNumber.ShouldEqual((long)state.FailedPartitions.ToArray()[0].SequenceNumber.Value);
    [Fact] void should_subscribe_with_event_types_for_failed_partition() => subscribed_token.EventTypes.ShouldEqual(event_types);
}
