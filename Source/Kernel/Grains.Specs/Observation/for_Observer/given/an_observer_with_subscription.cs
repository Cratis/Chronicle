// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.Runtime;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.given;

public class an_observer_with_subscription : an_observer
{
    protected ObserverSubscription subscription;
    protected ObserverSubscriberKey subscriber_key;

    void Establish()
    {
        subscription = new ObserverSubscription(observer_id, observer_key, [], typeof(ObserverSubscriber), SiloAddress.Zero, null);
        observer.SetSubscription(subscription);

        storage_stats.ResetCounts();
    }
}
