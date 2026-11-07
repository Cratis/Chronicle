// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_page_subscriber : an_observer_with_subscription_for_specific_event_type
{
    protected IObserverPartitionPageSubscriber _pageSubscriber;

    void Establish()
    {
        _pageSubscriber = Substitute.For<IObserverPartitionPageSubscriber>();
        _subscriber = _pageSubscriber;
        _silo.AddProbe(_ => _pageSubscriber);
        subscription = subscription with { SubscriberType = typeof(IObserverPartitionPageSubscriber) };
        _observer.SetSubscription(subscription);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL, LastHandledEventSequenceNumber = 9UL };
    }

    protected AppendedEvent EventAt(ulong sequenceNumber) => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, sequenceNumber);
}
