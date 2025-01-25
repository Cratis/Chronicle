// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.States;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.given;

public class an_observer_with_subscription : an_observer
{
    protected ObserverSubscription subscription;
    protected ObserverSubscriberKey subscriber_key;

    void Establish()
    {
        subscription = new ObserverSubscription(_observerId, _observerKey, [], typeof(ObserverSubscriber), SiloAddress.Zero, null);
        _observer.SetSubscription(subscription);
        _observer.TransitionTo<Routing>();

        _storageStats.ResetCounts();
    }
}
