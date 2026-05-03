// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_subscription_and_schema_for_event_type : an_observer_with_subscription_for_specific_event_type
{
    protected EventTypeSchema event_type_schema;

    async Task Establish()
    {
        event_type_schema = new EventTypeSchema(event_type, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema());

        _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns([event_type_schema]);

        await _observer.Subscribe<IObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero);

        _storageStats.ResetCounts();
        _failedPartitionsStorageStats.ResetCounts();
    }
}
