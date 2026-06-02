// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_querying_event_types_with_event_type_id;

public class when_filtering_event_types_by_collection_of_ids : given.an_event_store_db_context
{
    static readonly EventTypeId _firstId = new("first-type");
    static readonly EventTypeId _secondId = new("second-type");
    static readonly EventTypeId _thirdId = new("third-type");
    List<EventType> _results;

    void Establish()
    {
        context.EventTypes.AddRange(
            new EventType { Id = _firstId, Schemas = new Dictionary<uint, string>() },
            new EventType { Id = _secondId, Schemas = new Dictionary<uint, string>() },
            new EventType { Id = _thirdId, Schemas = new Dictionary<uint, string>() });
        context.SaveChanges();
    }

    async Task Because()
    {
        var ids = new[] { _firstId, _thirdId };
        _results = await context.EventTypes.Where(e => ids.Contains(e.Id)).ToListAsync();
    }

    [Fact] void should_return_only_matching_event_types() => _results.Count.ShouldEqual(2);
    [Fact] void should_include_first_event_type() => _results.ShouldContain(e => e.Id == _firstId);
    [Fact] void should_include_third_event_type() => _results.ShouldContain(e => e.Id == _thirdId);
    [Fact] void should_not_include_second_event_type() => _results.ShouldNotContain(e => e.Id == _secondId);
}
