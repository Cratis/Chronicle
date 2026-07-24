// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_resolving_schema_for_a_generation;

public class with_a_cold_cache : given.an_event_types_storage
{
    EventTypeSchema _result;

    async Task Because() => _result = await _storage.GetFor(_eventTypeId, _secondGeneration);

    [Fact] void should_return_the_requested_generation() => _result.Type.Generation.ShouldEqual(_secondGeneration);
    [Fact] void should_return_the_schema_for_the_requested_generation() => _result.Schema.ToJson().Contains(SecondGenerationProperty).ShouldBeTrue();
    [Fact] void should_not_return_the_schema_for_another_generation() => _result.Schema.ToJson().Contains(FirstGenerationProperty).ShouldBeFalse();
}
