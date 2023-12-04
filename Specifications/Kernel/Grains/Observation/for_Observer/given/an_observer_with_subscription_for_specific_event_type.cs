// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.given;

public class an_observer_with_subscription_for_specific_event_type : an_observer
{
    protected static readonly EventType event_type = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventGeneration.First);
    protected ObserverSubscription subscription;
    protected ObserverSubscriberKey subscriber_key;

    public an_observer_with_subscription_for_specific_event_type(OrleansClusterFixture clusterFixture)
        : base(clusterFixture)
    {
    }

    protected override void OnBeforeGrainActivate()
    {
        base.OnBeforeGrainActivate();

        subscription = new ObserverSubscription(GrainId, ObserverKey, new[] { event_type }, typeof(ObserverSubscriber), null);
        observer.SetSubscription(subscription);
    }
}
