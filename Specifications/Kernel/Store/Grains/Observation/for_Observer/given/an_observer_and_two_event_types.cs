// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.given;

public class an_observer_and_two_event_types : an_observer
{
    protected IEnumerable<EventType> event_types = new EventType[]
    {
        new("ad9f43ca-8d98-4669-99cd-dbd0eaaea9d9", 1),
        new("3e84ef60-c725-4b45-832d-29e3b663d7cd", 1)
    };

    void Establish() => state.EventTypes = event_types;
}
