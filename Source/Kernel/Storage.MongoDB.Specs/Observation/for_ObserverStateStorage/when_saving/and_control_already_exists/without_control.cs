// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving.and_control_already_exists;

public class without_control : given.an_observer_state_storage
{
    async Task Establish()
    {
        _state = _state with
        {
            TypeSetControl = new(
                new(7),
                new("reactor-subscriber", Concepts.Observation.ObserverFilters.None),
                new Dictionary<EventTypeId, ObserverTypeSetMember> { [new("event-a")] = new(new(3), new(42)) },
                [])
        };
        await _storage.Save(_state);
        _state = _state with { TypeSetControl = null };
    }

    async Task Because() => await _storage.Save(_state);

    [Fact] void should_not_replace_the_existing_control() => _set.Contains("typeSetControl").ShouldBeFalse();
    [Fact] void should_not_remove_the_existing_control() => _update.Names.ShouldEqual(["$set"]);
}
