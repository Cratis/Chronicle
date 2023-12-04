// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.given;

public class an_observer_with_subscription : an_observer
{
    protected ObserverSubscription subscription;
    protected ObserverSubscriberKey subscriber_key;

    void Establish()
    {
        subscription = new ObserverSubscription(observer_id, observer_key, Enumerable.Empty<EventType>(), typeof(ObserverSubscriber), null);
        observer.SetSubscription(subscription);

        silo.StorageStats().ResetCounts();
    }
}
