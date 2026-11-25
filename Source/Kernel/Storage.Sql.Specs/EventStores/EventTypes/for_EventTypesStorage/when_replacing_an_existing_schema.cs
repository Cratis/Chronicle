// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_replacing_an_existing_schema : given.an_event_types_storage
{
    JsonSchema _replacement;
    EventType _before;
    EventType _after;
    bool _changed;
    bool _repeat;
    bool _missingType;
    bool _missingGeneration;

    async Task Establish()
    {
        _replacement = await JsonSchema.FromJsonAsync("""{"title":"Keep","type":"object"}""");
        await using var context = CreateContext();
        _before = await context.EventTypes.SingleAsync();
        await _storage.GetDefinition(_eventTypeId);
    }

    async Task Because()
    {
        _changed = await _storage.ReplaceSchema(_eventTypeId, _firstGeneration, _replacement);
        _repeat = await _storage.ReplaceSchema(_eventTypeId, _firstGeneration, _replacement);
        _missingType = await _storage.ReplaceSchema("missing", 1, _replacement);
        _missingGeneration = await _storage.ReplaceSchema(_eventTypeId, 99, _replacement);
        await using var context = CreateContext();
        _after = await context.EventTypes.SingleAsync();
    }

    [Fact] void should_change_existing_schema() => _changed.ShouldBeTrue();
    [Fact] void should_not_repeat_mutation() => _repeat.ShouldBeFalse();
    [Fact] void should_not_create_type() => _missingType.ShouldBeFalse();
    [Fact] void should_not_create_generation() => _missingGeneration.ShouldBeFalse();
    [Fact] void should_store_exact_schema() => JsonNode.DeepEquals(JsonNode.Parse(_after.Schemas[_firstGeneration]), _replacement.Node).ShouldBeTrue();
    [Fact] void should_keep_other_generation() => _after.Schemas[_secondGeneration].ShouldEqual(_before.Schemas[_secondGeneration]);
    [Fact] void should_keep_owner() => _after.Owner.ShouldEqual(_before.Owner);
    [Fact] void should_keep_source() => _after.Source.ShouldEqual(_before.Source);
    [Fact] void should_keep_tombstone() => _after.Tombstone.ShouldEqual(_before.Tombstone);
    [Fact] void should_keep_origin() => _after.Origin.ShouldEqual(_before.Origin);
    [Fact] void should_keep_visibility() => _after.Visibility.ShouldEqual(_before.Visibility);
    [Fact] void should_keep_migrations() => _after.MigrationsJson.ShouldEqual(_before.MigrationsJson);
    [Fact] void should_keep_migration_versions() => _after.MigrationVersionsJson.ShouldEqual(_before.MigrationVersionsJson);
    [Fact] async Task should_evict_cached_definition() => JsonNode.DeepEquals((await _storage.GetDefinition(_eventTypeId)).Generations.Single(item => item.Generation == _firstGeneration).Schema.Node, _replacement.Node).ShouldBeTrue();
}
