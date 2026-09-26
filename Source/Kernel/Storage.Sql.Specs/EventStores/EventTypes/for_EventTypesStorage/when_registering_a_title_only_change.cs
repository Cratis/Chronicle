// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_a_title_only_change : given.an_event_types_storage
{
    bool _changed;
    string _stored;

    void Establish()
    {
        using var context = CreateContext();
        var stored = context.EventTypes.Single();
        stored.Owner = EventTypeOwner.Client;
        stored.Source = EventTypeSource.Code;
        context.SaveChanges();
    }

    async Task Because()
    {
        var incoming = await JsonSchema.FromJsonAsync(SchemaFor(FirstGenerationProperty).Replace("\"type\": \"object\"", "\"type\": \"object\", \"title\": \"NewTitle\""));
        _changed = await _storage.Register(new Concepts.Events.EventType(_eventTypeId, _firstGeneration), incoming);
        await using var context = CreateContext();
        _stored = (await context.EventTypes.SingleAsync()).Schemas[_firstGeneration];
    }

    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
    [Fact] void should_keep_the_stored_schema() => _stored.ShouldNotContain("NewTitle");
}
