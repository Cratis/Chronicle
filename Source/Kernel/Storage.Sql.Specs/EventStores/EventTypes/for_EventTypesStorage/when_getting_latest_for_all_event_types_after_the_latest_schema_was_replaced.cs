// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_getting_latest_for_all_event_types_after_the_latest_schema_was_replaced : given.an_event_types_storage
{
    const string ReplacedProperty = "replacedValue";

    EventTypeSchema[] _first;
    EventTypeSchema[] _second;

    async Task Because()
    {
        _first = [.. await _storage.GetLatestForAllEventTypes()];

        await using (var context = CreateContext())
        {
            var row = await context.EventTypes.SingleAsync();
            row.Schemas = new Dictionary<uint, string>(row.Schemas) { [_secondGeneration] = SchemaFor(ReplacedProperty) };
            await context.SaveChangesAsync();
        }

        _second = [.. await _storage.GetLatestForAllEventTypes()];
    }

    [Fact] void should_return_a_new_schema_instance() => ReferenceEquals(_first[0], _second[0]).ShouldBeFalse();
    [Fact] void should_return_the_replaced_schema() => _second[0].Schema.ToJson().Contains(ReplacedProperty).ShouldBeTrue();
    [Fact] void should_not_return_the_replaced_out_schema() => _second[0].Schema.ToJson().Contains(SecondGenerationProperty).ShouldBeFalse();
}
