// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_querying_event_types_with_event_type_id;

public class when_checking_if_event_type_exists : given.an_event_store_db_context
{
    static readonly EventTypeId _existing = new("known-event-type");
    static readonly EventTypeId _missing = new("unknown-event-type");
    bool _existingFound;
    bool _missingFound;

    void Establish()
    {
        context.EventTypes.Add(new EventType { Id = _existing, Schemas = new Dictionary<uint, string>() });
        context.SaveChanges();
    }

    async Task Because()
    {
        _existingFound = await context.EventTypes.AnyAsync(e => e.Id == _existing);
        _missingFound = await context.EventTypes.AnyAsync(e => e.Id == _missing);
    }

    [Fact] void should_find_existing_event_type() => _existingFound.ShouldBeTrue();
    [Fact] void should_not_find_missing_event_type() => _missingFound.ShouldBeFalse();
}
