// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverInformationForEventType.when_observing_observers_for_event_type;

public class and_the_subscriber_disposes : given.a_storage_with_two_namespaces
{
    int _activeWhileSubscribed;
    IEnumerable<ObserverInformationForEventType> _received;

    void Because()
    {
        var subscription = ObserverInformationForEventType
            .ObserveObserversForEventType(EventStoreName, EventTypeId, _storage)
            .Subscribe(_ => _received = _);
        _activeWhileSubscribed = _activeSubscriptions;
        subscription.Dispose();
    }

    [Fact] void should_have_subscribed_to_the_namespaces_and_each_namespace_while_subscribed() => _activeWhileSubscribed.ShouldBeGreaterThan(1);
    [Fact] void should_have_received_the_observers_of_every_namespace() => _received.Count().ShouldEqual(2);
    [Fact] void should_dispose_every_underlying_subscription() => _activeSubscriptions.ShouldEqual(0);
}
