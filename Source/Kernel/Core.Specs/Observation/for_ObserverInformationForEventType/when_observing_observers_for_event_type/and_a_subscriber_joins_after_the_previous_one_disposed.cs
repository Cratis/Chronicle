// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverInformationForEventType.when_observing_observers_for_event_type;

public class and_a_subscriber_joins_after_the_previous_one_disposed : given.a_storage_with_two_namespaces
{
    IEnumerable<ObserverInformationForEventType> _received;

    void Establish() => ObserverInformationForEventType
        .ObserveObserversForEventType(EventStoreName, EventTypeId, _storage)
        .Subscribe(_ => { })
        .Dispose();

    void Because()
    {
        ObserverInformationForEventType
            .ObserveObserversForEventType(EventStoreName, EventTypeId, _storage)
            .Subscribe(_ => _received = _);
        _definitionChanges.OnNext([]);
    }

    [Fact] void should_receive_the_change_after_the_initial_snapshot() => _received.ShouldBeEmpty();
}
