// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_conditionally_replacing_a_schema : given.an_event_types_storage
{
    bool _won;
    bool _stale;
    JsonSchema _expected;
    JsonSchema _replacement;

    async Task Establish()
    {
        _expected = (await _storage.GetFor(_eventTypeId, 1)).Schema;
        _replacement = await JsonSchema.FromJsonAsync(SchemaFor("added"));
    }

    async Task Because()
    {
        _won = await _storage.TryReplaceSchema(_eventTypeId, 1, _expected, _replacement);
        _stale = await _storage.TryReplaceSchema(_eventTypeId, 1, _expected, await JsonSchema.FromJsonAsync(SchemaFor("loser")));
    }

    [Fact] void should_apply_the_matching_schema() => _won.ShouldBeTrue();
    [Fact] void should_reject_a_stale_comparison() => _stale.ShouldBeFalse();
    [Fact] async Task should_refresh_cached_schema() => (await _storage.GetFor(_eventTypeId, 1)).Schema.Properties.ContainsKey("added").ShouldBeTrue();
    [Fact] async Task should_preserve_other_generations() => (await _storage.GetFor(_eventTypeId, 2)).Schema.Properties.ContainsKey(SecondGenerationProperty).ShouldBeTrue();
    [Fact] async Task should_not_create_missing_generations() => (await _storage.TryReplaceSchema(_eventTypeId, 99, _expected, _replacement)).ShouldBeFalse();
}
