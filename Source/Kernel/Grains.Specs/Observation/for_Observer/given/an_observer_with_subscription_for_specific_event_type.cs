// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.Runtime;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.given;

public class an_observer_with_subscription_for_specific_event_type : an_observer
{
    protected static readonly EventType event_type = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    protected ObserverSubscription subscription;
    protected ObserverSubscriberKey subscriber_key;

    void Establish()
    {
        subscription = new ObserverSubscription(observer_id, observer_key, [event_type], typeof(IObserverSubscriber), SiloAddress.Zero, null);
        observer.SetSubscription(subscription);

        storage_stats.ResetCounts();
    }
}
