// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_conditionally_replacing_a_schema : given.an_event_type_with_two_generations
{
    bool _won;
    bool _stale;
    JsonSchema _expected;
    JsonSchema _replacement;

    async Task Establish()
    {
        _expected = (await _storage.GetFor(first_generation.Id, 1)).Schema;
        _replacement = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"added":{"type":"string"}}}""");
    }

    async Task Because()
    {
        _won = await _storage.TryReplaceSchema(first_generation.Id, 1, _expected, _replacement);
        _stale = await _storage.TryReplaceSchema(first_generation.Id, 1, _expected, await JsonSchema.FromJsonAsync("{}"));
    }

    [Fact] void should_apply_the_matching_schema() => _won.ShouldBeTrue();
    [Fact] void should_reject_a_stale_comparison() => _stale.ShouldBeFalse();
    [Fact] async Task should_read_the_new_schema() => (await _storage.GetFor(first_generation.Id, 1)).Schema.Properties.ContainsKey("added").ShouldBeTrue();
    [Fact] async Task should_preserve_other_generations() => (await _storage.GetFor(first_generation.Id, 2)).Schema.Description.ShouldEqual("second");
    [Fact] async Task should_not_create_missing_generations() => (await _storage.TryReplaceSchema(first_generation.Id, 99, _expected, _replacement)).ShouldBeFalse();
}
