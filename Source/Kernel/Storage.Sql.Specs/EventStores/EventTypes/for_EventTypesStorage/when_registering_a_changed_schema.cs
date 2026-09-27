// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_a_changed_schema : given.an_event_types_storage
{
    bool _changed;
    string _stored;

    async Task Because()
    {
        _changed = await _storage.Register(
            new Concepts.Events.EventType(_eventTypeId, _firstGeneration),
            await JsonSchema.FromJsonAsync(SchemaFor("aDifferentProperty")));
        await using var context = CreateContext();
        _stored = (await context.EventTypes.SingleAsync()).Schemas[_firstGeneration];
    }

    [Fact] void should_report_a_change() => _changed.ShouldBeTrue();
    [Fact] void should_store_the_changed_schema() => _stored.ShouldContain("aDifferentProperty");
}
