// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Replay;

public class when_replaying_with_failure : given.a_replay_with_two_pending_events
{
    IEnumerable<FailedPartition> failed_partitions;

    void Establish()
    {
        subscriber.SetupSequence(_ => _.OnNext(IsAny<AppendedEvent>(), IsAny<ObserverSubscriberContext>()))
            .Returns(Task.FromResult(ObserverSubscriberResult.Ok))
            .Returns(Task.FromResult(ObserverSubscriberResult.Failed));

        supervisor.Setup(_ => _.NotifyReplayComplete(IsAny<IEnumerable<FailedPartition>>())).Callback((IEnumerable<FailedPartition> partitions) => failed_partitions = partitions);
    }

    Task Because() => replay.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), event_types, typeof(ObserverSubscriber), subscriber_args));

    [Fact] void should_notify_supervisor_that_replay_is_complete_with_failed_partition() => failed_partitions.Count().ShouldEqual(1);
    [Fact] void should_have_correct_partition() => failed_partitions.First().Partition.ShouldEqual(second_event_source_id);
    [Fact] void should_have_correct_tail() => failed_partitions.First().Tail.ShouldEqual(second_appended_event.Metadata.SequenceNumber);
}
