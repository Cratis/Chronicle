// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs.for_CatchUpObserver.when_asking_if_can_resume;

public class and_it_is_subscribed : given.a_catchup_observer_and_a_request
{
    Mock<IObserver> observer;
    bool result;

    void Establish()
    {
        observer = silo.AddProbe<IObserver>(((CatchUpObserverRequest)state_storage.State.Request).ObserverId, ((CatchUpObserverRequest)state_storage.State.Request).ObserverKey);
        observer.Setup(_ => _.IsSubscribed()).ReturnsAsync(true);
    }

    async Task Because() => result = await job.WrappedCanResume();

    [Fact] void should_be_able_to_resume() => result.ShouldBeTrue();
}
