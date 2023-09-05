// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Replay;

public class when_replaying_with_no_events_to_replay : given.a_replay_worker
{
    Task Because() => replay.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), Enumerable.Empty<EventType>(), typeof(ObserverSubscriber), subscriber_args));

    [Fact] void should_notify_supervisor_that_replay_is_complete() => supervisor.Verify(_ => _.NotifyReplayComplete(IsAny<IEnumerable<FailedPartition>>()), Once);
}
