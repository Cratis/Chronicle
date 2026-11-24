// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_reading;

public class the_same_generation_twice : given.a_registry
{
    EventTypeSchema _first;
    EventTypeSchema _second;
    EventTypeSchema _latestFromList;
    EventTypeSchema _latestByGeneration;

    async Task Because()
    {
        _first = await _registry.GetFor(_eventTypeId, 1);
        _second = await _registry.GetFor(_eventTypeId, 1);
        _latestFromList = (await _registry.GetLatestForAllEventTypes()).Single(_ => _.Type.Id == _eventTypeId);
        _latestByGeneration = await _registry.GetFor(_eventTypeId, 2);
    }

    [Fact] void should_share_the_schema_instance() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_share_the_parsed_json_schema() => ReferenceEquals(_first.Schema, _second.Schema).ShouldBeTrue();
    [Fact] void should_use_the_same_instance_for_the_latest_generation() => ReferenceEquals(_latestFromList, _latestByGeneration).ShouldBeTrue();
    [Fact] void should_hold_the_schema_of_that_generation() => _first.Schema.Description.ShouldEqual("first");
}
