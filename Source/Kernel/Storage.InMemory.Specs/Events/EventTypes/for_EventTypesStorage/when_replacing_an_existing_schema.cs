// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_replacing_an_existing_schema : given.an_event_type_with_two_generations
{
    JsonSchema _replacement;
    bool _changed;
    bool _repeat;
    bool _missingType;
    bool _missingGeneration;

    async Task Establish() => _replacement = await JsonSchema.FromJsonAsync("""{"title":"Keep","type":"object"}""");

    async Task Because()
    {
        _changed = await _storage.ReplaceSchema(first_generation.Id, first_generation.Generation, _replacement);
        _repeat = await _storage.ReplaceSchema(first_generation.Id, first_generation.Generation, _replacement);
        _missingType = await _storage.ReplaceSchema("missing", 1, _replacement);
        _missingGeneration = await _storage.ReplaceSchema(first_generation.Id, 99, _replacement);
    }

    [Fact] void should_change_existing_schema() => _changed.ShouldBeTrue();
    [Fact] void should_not_repeat_mutation() => _repeat.ShouldBeFalse();
    [Fact] void should_not_create_type() => _missingType.ShouldBeFalse();
    [Fact] void should_not_create_generation() => _missingGeneration.ShouldBeFalse();
    [Fact] async Task should_store_exact_schema() => JsonNode.DeepEquals(JsonNode.Parse((await _storage.GetFor(first_generation.Id, 1)).Schema.ToJson()), JsonNode.Parse(_replacement.ToJson())).ShouldBeTrue();
    [Fact] async Task should_keep_other_generation() => (await _storage.GetFor(first_generation.Id, 2)).Schema.Description.ShouldEqual("second");
}
