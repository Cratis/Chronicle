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

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.given;

public abstract class a_read_model_joined_on_a_source<THarness> : Specification
    where THarness : ISinkHarness, new()
{
    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "joinId": { "type": "string" },
            "name": { "type": "string" },
            "owner": { "type": "object", "properties": { "id": { "type": "string" }, "name": { "type": "string" } } }
          }
        }
        """;

    protected ISink _sink;
    THarness _harness;

    async Task Establish()
    {
        _harness = CreateHarness();
        _sink = _harness.CreateSink(new ReadModelDefinition(
            "joined_roots",
            "joined_roots",
            "Joined roots",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = await JsonSchema.FromJsonAsync(SchemaJson) },
            []));
        dynamic owner = new ExpandoObject();
        owner.id = "owner-1";
        owner.name = "Owner";
        Change[] changes =
        [
            new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [
                new PropertyDifference(new PropertyPath("joinId"), null, "source"),
                new PropertyDifference(new PropertyPath("name"), null, "before"),
                new PropertyDifference(new PropertyPath("owner"), null, (ExpandoObject)owner)
            ])
        ];
        await _sink.ApplyChanges(new Key("root", ArrayIndexers.NoIndexers), Changeset(new ExpandoObject(), changes), 1UL);
    }

    void Destroy() => _harness.Dispose();

    /// <summary>
    /// Creates the harness supplying the implementation under specification.
    /// </summary>
    /// <returns>The harness to run the contract through.</returns>
    protected virtual THarness CreateHarness() => new();

    protected async Task<IDictionary<string, object?>?> Find(string id) =>
        await _sink.FindOrDefault(new Key(id, ArrayIndexers.NoIndexers));

    protected static IChangeset<AppendedEvent, ExpandoObject> Changeset(ExpandoObject initial, Change[] changes)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initial);
        changeset.Changes.Returns(changes);
        return changeset;
    }
}
