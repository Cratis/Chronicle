// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_CatchUp;

public class when_catching_up_with_no_events_to_catch_up_to : given.a_catch_up_worker
{

    Task Because() => catch_up.Start(new(typeof(ObserverSubscriber), null!));

    [Fact] void should_notify_supervisor_that_catch_up_is_complete() => supervisor.Verify(_ => _.NotifyCatchUpComplete(), Once);
}
