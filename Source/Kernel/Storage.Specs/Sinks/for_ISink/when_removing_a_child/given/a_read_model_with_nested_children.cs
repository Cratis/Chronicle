// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_child.given;

/// <summary>
/// The arrangement every nested child removal case shares: one read model holding children at the root, in parents
/// and in parents inside groups, written and read back through whichever sink the harness supplies.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class a_read_model_with_nested_children<THarness> : Specification
    where THarness : ISinkHarness, new()
{
    protected const string DocumentKey = "document";

    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "children": { "type": "array", "items": { "type": "object", "properties": { "id": { "type": "string" } } } },
            "parents": { "type": "array", "items": { "type": "object", "properties": {
              "id": { "type": "string" },
              "children": { "type": "array", "items": { "type": "object", "properties": { "id": { "type": "string" } } } }
            } } },
            "groups": { "type": "array", "items": { "type": "object", "properties": {
              "id": { "type": "string" },
              "parents": { "type": "array", "items": { "type": "object", "properties": {
                "id": { "type": "string" },
                "children": { "type": "array", "items": { "type": "object", "properties": { "id": { "type": "string" } } } }
              } } }
            } } }
          }
        }
        """;

    protected ISink _sink;
    protected IDictionary<string, object?> _stored;

    THarness _harness;

    void Establish()
    {
        _harness = CreateHarness();
        _sink = _harness.CreateSink(CreateReadModelDefinition());
    }

    void Destroy() => _harness.Dispose();

    /// <summary>
    /// Creates the harness supplying the implementation under specification.
    /// </summary>
    /// <returns>The <typeparamref name="THarness"/> to run the contract through.</returns>
    /// <remarks>
    /// Overridable because a backend needing infrastructure receives it through the constructor - a
    /// container fixture, say - and so cannot be built by the contract itself.
    /// </remarks>
    protected virtual THarness CreateHarness() => new();

    protected static ExpandoObject Item(string id, params (string Name, object Value)[] members)
    {
        var item = new ExpandoObject();
        var values = (IDictionary<string, object?>)item;
        values["id"] = id;
        foreach (var (name, value) in members)
        {
            values[name] = value;
        }

        return item;
    }

    protected static ArrayIndexer Indexer(string path, string identifier) => new(path, "id", identifier);

    protected static IEnumerable<string> Ids(object? collection) =>
        ((IEnumerable<object>)collection!).Cast<IDictionary<string, object?>>().Select(item => (string)item["id"]!).ToArray();

    protected static IDictionary<string, object?> Find(object? collection, string id) =>
        ((IEnumerable<object>)collection!).Cast<IDictionary<string, object?>>().Single(item => (string)item["id"]! == id);

    protected Task Seed(string collection, params ExpandoObject[] items)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(new PropertyPath(collection), null, items.ToList())])];
        changeset.Changes.Returns(changes);
        return _sink.ApplyChanges(new Key(DocumentKey, ArrayIndexers.NoIndexers), changeset, 1UL);
    }

    protected async Task RemoveChild(string childrenProperty, string childKey, params ArrayIndexer[] indexers)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new ChildRemoved(Item(childKey), childrenProperty, "id", childKey)];
        changeset.Changes.Returns(changes);

        await _sink.ApplyChanges(new Key(DocumentKey, new ArrayIndexers(indexers)), changeset, 2UL);
        _stored = (await _sink.FindOrDefault(new Key(DocumentKey, ArrayIndexers.NoIndexers)))!;
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "documents",
            "documents",
            "Documents",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromJson(SchemaJson) },
            []);
}
