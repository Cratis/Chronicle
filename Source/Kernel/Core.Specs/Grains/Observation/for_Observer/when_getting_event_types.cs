// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_getting_event_types : given.an_observer
{
    public IEnumerable<EventType> eventTypes;

    async Task Because() => eventTypes = await _observer.GetEventTypes();

    [Fact] void should_contain_the_correct_event_types() => eventTypes.ShouldContainOnly(_definitionStorage.State.EventTypes);
}
