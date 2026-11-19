// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_getting_latest_for_all_event_types_after_the_event_type_was_tombstoned : given.an_event_types_storage
{
    EventTypeSchema[] _first;
    EventTypeSchema[] _second;

    async Task Because()
    {
        _first = [.. await _storage.GetLatestForAllEventTypes()];

        await using (var context = CreateContext())
        {
            var row = await context.EventTypes.SingleAsync();
            row.Tombstone = true;
            await context.SaveChangesAsync();
        }

        _second = [.. await _storage.GetLatestForAllEventTypes()];
    }

    [Fact] void should_not_have_been_tombstoned_before() => _first[0].Type.Tombstone.ShouldBeFalse();
    [Fact] void should_return_a_new_schema_instance() => ReferenceEquals(_first[0], _second[0]).ShouldBeFalse();
    [Fact] void should_return_the_tombstoned_event_type() => _second[0].Type.Tombstone.ShouldBeTrue();
}
