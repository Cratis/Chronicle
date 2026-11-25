// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_getting_latest_for_all_event_types_after_a_generation_was_added : given.an_event_types_storage
{
    const string ThirdGenerationProperty = "thirdGenerationValue";
    static readonly EventTypeGeneration _thirdGeneration = new(3);

    EventTypeSchema[] _first;
    EventTypeSchema[] _second;

    async Task Because()
    {
        _first = [.. await _storage.GetLatestForAllEventTypes()];

        await using (var context = CreateContext())
        {
            var row = await context.EventTypes.SingleAsync();
            row.Schemas = new Dictionary<uint, string>(row.Schemas) { [_thirdGeneration] = SchemaFor(ThirdGenerationProperty) };
            await context.SaveChangesAsync();
        }

        _second = [.. await _storage.GetLatestForAllEventTypes()];
    }

    [Fact] void should_return_a_new_schema_instance() => ReferenceEquals(_first[0], _second[0]).ShouldBeFalse();
    [Fact] void should_return_the_new_generation() => _second[0].Type.Generation.ShouldEqual(_thirdGeneration);
    [Fact] void should_return_the_schema_of_the_new_generation() => _second[0].Schema.ToJson().Contains(ThirdGenerationProperty).ShouldBeTrue();
}
