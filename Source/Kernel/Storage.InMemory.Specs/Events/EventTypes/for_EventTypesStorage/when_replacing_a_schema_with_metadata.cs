// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_replacing_a_schema_with_metadata : given.an_event_types_storage
{
    EventTypeDefinition _before;
    EventTypeDefinition _after;
    EventTypeSchema _metadataBefore;
    EventTypeSchema _metadataAfter;
    IDisposable _subscription;
    int _notifications;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"deprecated":false,"type":"object"}""");
        await _storage.Register(new EventType("test", 1, true), schema, EventTypeOwner.Server, EventTypeSource.Code, EventTypeVisibility.Unspecified, "original");
        await _storage.Register(new EventTypeDefinition("test", EventTypeOwner.Server, true, [new(1, schema), new(2, schema)], [new(1, 2, [], new JsonObject(), new JsonObject())]));
        _before = await _storage.GetDefinition("test");
        _metadataBefore = await _storage.GetFor("test", 1);
        _subscription = _storage.ObserveLatestForAllEventTypes().Subscribe(_ => _notifications++);
    }

    async Task Because()
    {
        var replacement = await JsonSchema.FromJsonAsync("""{"type":"object"}""");
        await _storage.ReplaceSchema("test", 1, replacement);
        await _storage.ReplaceSchema("test", 1, replacement);
        _after = await _storage.GetDefinition("test");
        _metadataAfter = await _storage.GetFor("test", 1);
    }

    void Destroy() => _subscription.Dispose();

    [Fact] void should_keep_generation_count() => _after.Generations.Count().ShouldEqual(_before.Generations.Count());
    [Fact] void should_keep_owner() => _after.Owner.ShouldEqual(_before.Owner);
    [Fact] void should_keep_tombstone() => _after.Tombstone.ShouldEqual(_before.Tombstone);
    [Fact] void should_keep_migrations() => _after.Migrations.ShouldEqual(_before.Migrations);
    [Fact] void should_keep_source() => _metadataAfter.Source.ShouldEqual(_metadataBefore.Source);
    [Fact] void should_keep_visibility() => _metadataAfter.Visibility.ShouldEqual(_metadataBefore.Visibility);
    [Fact] void should_keep_origin() => _metadataAfter.Origin.ShouldEqual(_metadataBefore.Origin);
    [Fact] void should_notify_only_for_mutation() => _notifications.ShouldEqual(2);
}
