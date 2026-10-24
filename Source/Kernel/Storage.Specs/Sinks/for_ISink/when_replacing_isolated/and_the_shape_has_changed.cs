// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_the_shape_has_changed<THarness> : given.an_isolated_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    IDictionary<string, object?> _stored;

    async Task Establish()
    {
        dynamic original = State(10);
        original.obsolete = "remove me";
        original.details = new ExpandoObject();
        original.details.name = "old";
        original.details.obsolete = "remove me too";
        dynamic child = new ExpandoObject();
        child.id = "old";
        original.children = new[] { child };
        await _sink.ReplaceIsolated(_key, original, 10);
    }

    async Task Because()
    {
        dynamic replacement = State(20);
        replacement.details = new ExpandoObject();
        replacement.details.name = "new";
        dynamic child = new ExpandoObject();
        child.id = "new";
        replacement.children = new[] { child };
        await _sink.ReplaceIsolated(_key, replacement, 20);
        _stored = (await _sink.FindOrDefault(_key))!;
    }

    [Fact] public void should_remove_the_obsolete_scalar() => (_stored.TryGetValue("obsolete", out var value) && value is not null).ShouldBeFalse();
    [Fact] public void should_replace_the_nested_object() => ((IDictionary<string, object?>)_stored["details"]!)["name"].ShouldEqual("new");
    [Fact] public void should_remove_the_obsolete_nested_field() => ((IDictionary<string, object?>)_stored["details"]!).ContainsKey("obsolete").ShouldBeFalse();
    [Fact] public void should_replace_the_children() => ((IDictionary<string, object?>)((IEnumerable<object>)_stored["children"]!).Single())["id"].ShouldEqual("new");

    protected override ReadModelDefinition CreateReadModelDefinition() => base.CreateReadModelDefinition() with
    {
        Schemas = new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [ReadModelGeneration.First] = JsonSchema.FromJson("""
                {
                  "type": "object",
                  "properties": {
                    "count": { "type": "integer" },
                    "obsolete": { "type": ["string", "null"] },
                    "details": { "type": "object", "properties": { "name": { "type": "string" }, "obsolete": { "type": "string" } } },
                    "children": { "type": "array", "items": { "type": "object", "properties": { "id": { "type": "string" } } } }
                  }
                }
                """)
        }
    };
}
