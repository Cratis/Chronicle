// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_CatchUp;

public class when_catching_up_with_failure : given.a_catch_up_worker_with_two_pending_events
{
    void Establish()
    {
        subscriber.SetupSequence(_ => _.OnNext(IsAny<AppendedEvent>(), IsAny<ObserverSubscriberContext>()))
            .Returns(Task.FromResult(ObserverSubscriberResult.Ok))
            .Returns(Task.FromResult(ObserverSubscriberResult.Failed));
    }

    Task Because() => catch_up.Start(new(typeof(ObserverSubscriber), null!));

    [Fact] void should_notify_supervisor_that_partition_has_failed_for_second_event() => supervisor.Verify(_ => _.PartitionFailed(second_appended_event, IsAny<IEnumerable<string>>(), IsAny<string>()), Once);
    [Fact] void should_notify_supervisor_that_catch_up_is_complete() => supervisor.Verify(_ => _.NotifyCatchUpComplete(), Once);
}
