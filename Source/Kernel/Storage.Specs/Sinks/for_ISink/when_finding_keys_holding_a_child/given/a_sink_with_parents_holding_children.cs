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

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_keys_holding_a_child.given;

/// <summary>
/// Base for the specs of the read-only lookup of the parents that hold a child.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the sink under specification.</typeparam>
public abstract class a_sink_with_parents_holding_children<THarness> : Specification
    where THarness : ISinkHarness, new()
{
    protected const string ChildId = "childId";
    protected const string RemovedChild = "removed-child";
    protected const string Initial = "initial";

    protected THarness _harness;
    protected ISink _sink;
    protected IReadOnlyCollection<Key> _keys;

    /// <summary>
    /// Gets the path of the children collection.
    /// </summary>
    protected virtual string ChildrenPath => "children";

    /// <summary>
    /// Gets the JSON schema of the parents.
    /// </summary>
    protected virtual string SchemaJson => """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "lastUpdated": { "type": "string" },
            "children": { "type": "array", "items": { "type": "object", "properties": { "childId": { "type": "string" } } } }
          }
        }
        """;

    protected ChildRemovedFromAll Removal(string childId = RemovedChild) =>
        new(new PropertyPath(ChildrenPath), new PropertyPath(ChildId), childId, ArrayIndexers.NoIndexers);

    protected async Task SeedParents()
    {
        _harness = CreateHarness();
        _sink = _harness.CreateSink(CreateReadModelDefinition());
        await Seed("first", RemovedChild, "kept-on-first");
        await Seed("second", RemovedChild);
        await Seed("unrelated", "other-child");
    }

    protected async Task<IEnumerable<string>> ChildIdsOf(string parent)
    {
        var document = await _sink.FindOrDefault(new Key(parent, ArrayIndexers.NoIndexers));
        return ChildrenOf(document!).Select(child => (string)child[ChildId]!).ToArray();
    }

    protected IEnumerable<string> KeyValues() => _keys.Select(key => key.Value.ToString()!).Order().ToArray();

    protected virtual IEnumerable<IDictionary<string, object?>> ChildrenOf(IDictionary<string, object?> parent) =>
        ((IEnumerable<object>)parent["children"]!).Cast<IDictionary<string, object?>>();

    protected virtual IEnumerable<PropertyDifference> ChildDifferences(List<ExpandoObject> children) =>
        [new PropertyDifference(new PropertyPath("children"), null, children)];

    /// <summary>
    /// Creates the harness supplying the implementation under specification.
    /// </summary>
    /// <returns>The <typeparamref name="THarness"/> to run the contract through.</returns>
    protected virtual THarness CreateHarness() => new();

    void Destroy() => _harness.Dispose();

    static ExpandoObject Child(string id)
    {
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)[ChildId] = id;
        return child;
    }

    ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "parents",
            "parents",
            "Parents",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromJson(SchemaJson) },
            []);

    async Task Seed(string parent, params string[] childIds)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes =
        [
            new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [
                new PropertyDifference(new PropertyPath("lastUpdated"), null, Initial),
                .. ChildDifferences([.. childIds.Select(Child)])
            ])
        ];
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(new Key(parent, ArrayIndexers.NoIndexers), changeset, 1UL);
    }
}
