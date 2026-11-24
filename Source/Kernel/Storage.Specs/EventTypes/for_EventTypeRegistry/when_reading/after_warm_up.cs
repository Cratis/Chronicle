// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_reading;

public class after_warm_up : given.a_registry
{
    int _readsAfterWarmUp;
    int _readsAfterwards;
    IEnumerable<EventTypeSchema> _latest;

    async Task Because()
    {
        await _registry.GetLatestForAllEventTypes();
        _readsAfterWarmUp = _storage.Reads;

        _latest = await _registry.GetLatestForAllEventTypes();
        await _registry.GetAllDefinitions();
        await _registry.GetDefinition(_eventTypeId);
        await _registry.GetFor(_eventTypeId, 1);
        await _registry.GetFor(_eventTypeId, 2);
        await _registry.HasFor(_eventTypeId, 2);
        await _registry.GetFor([new EventType(_eventTypeId, 1), new EventType(_otherEventTypeId, 1)]);
        _readsAfterwards = _storage.Reads;
    }

    [Fact] void should_have_loaded_with_a_few_reads() => _readsAfterWarmUp.ShouldBeGreaterThan(0);
    [Fact] void should_not_read_storage_again() => _readsAfterwards.ShouldEqual(_readsAfterWarmUp);
    [Fact] void should_list_every_event_type() => _latest.Select(_ => _.Type.Id).ShouldContainOnly(_eventTypeId, _otherEventTypeId);
    [Fact] void should_list_the_latest_generation() => _latest.Single(_ => _.Type.Id == _eventTypeId).Type.Generation.Value.ShouldEqual(2u);
}
