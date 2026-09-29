// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.when_getting_all_generations_for_an_event_type;

public class and_it_has_two_generations : given.an_event_type_with_two_generations_in_the_database
{
    EventTypeSchema[] _result;

    async Task Because() => _result = [.. await _storage.GetAllGenerationsForEventType(new EventType(_eventTypeId, _firstGeneration))];

    [Fact] void should_return_one_schema_per_generation() => _result.Length.ShouldEqual(2);
    [Fact] void should_return_the_first_generation_with_its_schema() =>
        _result.Single(_ => _.Type.Generation == _firstGeneration).Schema.Properties.ContainsKey(FirstGenerationProperty).ShouldBeTrue();
    [Fact] void should_return_the_second_generation_with_its_schema() =>
        _result.Single(_ => _.Type.Generation == _secondGeneration).Schema.Properties.ContainsKey(SecondGenerationProperty).ShouldBeTrue();
}
