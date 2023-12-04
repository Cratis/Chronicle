// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.given;

public class an_observer_with_subscription : an_observer
{
    protected ObserverSubscription subscription;
    protected ObserverSubscriberKey subscriber_key;

    public an_observer_with_subscription(OrleansClusterFixture clusterFixture)
        : base(clusterFixture)
    {
    }

    protected override void OnBeforeGrainActivate()
    {
        base.OnBeforeGrainActivate();

        subscription = new ObserverSubscription(GrainId, ObserverKey, Enumerable.Empty<EventType>(), typeof(ObserverSubscriber), null);
        observer.SetSubscription(subscription);
    }
}
