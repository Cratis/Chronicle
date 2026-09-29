// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_getting_latest_for_all_event_types : given.an_event_types_storage
{
    EventTypeSchema[] _result;

    async Task Because() => _result = [.. await _storage.GetLatestForAllEventTypes()];

    [Fact] void should_return_one_schema_for_the_event_type() => _result.Length.ShouldEqual(1);
    [Fact] void should_return_the_latest_generation() => _result[0].Type.Generation.ShouldEqual(_secondGeneration);
    [Fact] void should_return_the_schema_of_the_latest_generation() => _result[0].Schema.ToJson().Contains(SecondGenerationProperty).ShouldBeTrue();
    [Fact] void should_not_return_the_schema_of_an_earlier_generation() => _result[0].Schema.ToJson().Contains(FirstGenerationProperty).ShouldBeFalse();
}
