// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_event_types_have_known_schemas : given.an_observer
{
    EventType _event_type;
    EventTypeSchema _schema;

    void Establish()
    {
        _event_type = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
        _schema = new EventTypeSchema(_event_type, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema());
        _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns([_schema]);
    }

    async Task Because() => await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_event_type], SiloAddress.Zero);

    [Fact] void should_fetch_schemas_for_the_subscribed_event_types() => _eventTypesStorage.Received(1).GetFor(Arg.Any<IEnumerable<EventType>>());
}
