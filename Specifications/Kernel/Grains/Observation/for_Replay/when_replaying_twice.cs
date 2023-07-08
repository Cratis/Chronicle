// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Replay;

public class when_replaying_twice : given.a_replay_with_two_pending_events
{
    void Establish()
    {
        timer_registry
            .Setup(_ => _.RegisterTimer(grain, IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()))
            .Returns((Grain __, Func<object, Task> _, object _____, TimeSpan ___, TimeSpan ____) => Task.CompletedTask);
    }

    async Task Because()
    {
        await replay.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), Enumerable.Empty<EventType>(), typeof(ObserverSubscriber), null!));
        await replay.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), Enumerable.Empty<EventType>(), typeof(ObserverSubscriber), null!));
    }

    [Fact] void should_not_start_more_than_once() => timer_registry.Verify(_ => _.RegisterTimer(grain, IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()), Once);
}
