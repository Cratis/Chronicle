// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.given;

public abstract class a_read_model_with_nested_values<THarness> : Specification
    where THarness : ISinkHarness, new()
{
    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "count": { "type": "integer" },
            "enabled": { "type": "boolean" },
            "name": { "type": ["string", "null"] },
            "child": { "type": "object", "properties": { "name": { "type": "string" }, "age": { "type": "integer" } } },
            "children": { "type": "array", "items": { "type": "object", "properties": { "childId": { "type": "string" } } } }
          }
        }
        """;

    protected ISink _sink;
    protected Key _key;
    protected IsolatedReplaceResult _result;
    protected IDictionary<string, object?> _stored;

    THarness _harness;

    async Task Establish()
    {
        _key = new Key("root", ArrayIndexers.NoIndexers);
        _harness = CreateHarness();
        _sink = _harness.CreateSink(new ReadModelDefinition(
            "nested_values",
            "nested_values",
            "Nested values",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromJson(SchemaJson) },
            []));
        dynamic child = new ExpandoObject();
        child.name = "before";
        child.age = 7;
        dynamic item = new ExpandoObject();
        item.childId = "old-child";
        dynamic state = new ExpandoObject();
        state.count = 7;
        state.enabled = true;
        state.name = "before";
        state.child = (ExpandoObject)child;
        state.children = new List<ExpandoObject> { (ExpandoObject)item };
        await _sink.ReplaceIsolated(_key, (ExpandoObject)state, 1UL);
    }

    void Destroy() => _harness.Dispose();

    /// <summary>
    /// Creates the harness supplying the implementation under specification.
    /// </summary>
    /// <returns>The harness to run the contract through.</returns>
    protected virtual THarness CreateHarness() => new();

    protected async Task ReplaceWith(ExpandoObject state)
    {
        _result = await _sink.ReplaceIsolated(_key, state, 2UL);
        _stored = (await _sink.FindOrDefault(_key))!;
    }

    protected object? StoredValue(string name) => _stored.TryGetValue(name, out var value) ? value : null;

    protected object? NestedValue(string name) =>
        StoredValue("child") is IDictionary<string, object?> child && child.TryGetValue(name, out var value) ? value : null;
}
