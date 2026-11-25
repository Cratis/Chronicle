// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_two_observers_share_event_types : given.a_registry
{
    ObserverDefinition _first;
    ObserverDefinition _second;

    async Task Because()
    {
        _first = await _registry.Get(_observerId);
        _second = await _registry.Get(_otherObserverId);
    }

    [Fact] void should_hold_the_event_types_as_a_set() => (_first.EventTypes is EventTypeSet).ShouldBeTrue();
    [Fact] void should_share_the_event_type_set() => ReferenceEquals(_first.EventTypes, _second.EventTypes).ShouldBeTrue();
}
