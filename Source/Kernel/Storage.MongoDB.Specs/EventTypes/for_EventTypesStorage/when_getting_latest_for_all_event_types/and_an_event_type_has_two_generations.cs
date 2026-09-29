// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.when_getting_latest_for_all_event_types;

public class and_an_event_type_has_two_generations : given.an_event_type_with_two_generations_in_the_database
{
    EventTypeSchema[] _result;

    async Task Because() => _result = [.. await _storage.GetLatestForAllEventTypes()];

    [Fact] void should_return_one_schema_for_the_event_type() => _result.Length.ShouldEqual(1);
    [Fact] void should_return_the_latest_generation() => _result[0].Type.Generation.ShouldEqual(_secondGeneration);
    [Fact] void should_return_the_schema_of_the_latest_generation() => _result[0].Schema.Properties.ContainsKey(SecondGenerationProperty).ShouldBeTrue();
    [Fact] void should_not_return_the_schema_of_an_earlier_generation() => _result[0].Schema.Properties.ContainsKey(FirstGenerationProperty).ShouldBeFalse();
}
