// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.when_resolving;

public class an_unknown_event_type : given.an_event_types_storage
{
    EventTypeSchema _single;
    IEnumerable<EventTypeSchema> _byGeneration;
    IEnumerable<EventTypeSchema> _byId;
    bool _has;

    async Task Because()
    {
        _single = await _storage.GetFor("never-registered", 3);
        _byGeneration = await _storage.GetFor([new EventType("never-registered", 1)]);
        _byId = await _storage.GetFor([new EventTypeId("never-registered")]);
        _has = await _storage.HasFor("never-registered");
    }

    [Fact] void should_resolve_an_empty_schema_for_the_single_lookup() => _single.Schema.Properties.ShouldBeEmpty();
    [Fact] void should_keep_the_requested_generation_for_the_single_lookup() => _single.Type.Generation.Value.ShouldEqual(3u);
    [Fact] void should_omit_it_from_the_batch_by_generation() => _byGeneration.ShouldBeEmpty();
    [Fact] void should_omit_it_from_the_batch_by_id() => _byId.ShouldBeEmpty();
    [Fact] void should_report_that_it_exists() => _has.ShouldBeTrue();
}
