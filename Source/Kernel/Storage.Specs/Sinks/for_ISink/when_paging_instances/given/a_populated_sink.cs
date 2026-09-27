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
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

public abstract class a_populated_sink<THarness> : Specification
    where THarness : ISinkHarness, new()
{
    protected ISink _sink;
    THarness _harness;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            { "type": "object", "properties": { "id": { "type": "string" }, "name": { "type": "string" } } }
            """);
        _harness = new THarness();
        _sink = _harness.CreateSink(new ReadModelDefinition(
            "test-read-model",
            "paged_read_models",
            "Paged read models",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []));

        foreach (var name in (string[])["d", "b", "a", "c", "e"])
        {
            var state = new ExpandoObject();
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.InitialState.Returns(state);
            changeset.Changes.Returns((Change[])[
                new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference(new PropertyPath("name"), null, name)])
            ]);
            await _sink.ApplyChanges(new Key(name, ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.First);
        }
    }

    void Destroy() => _harness.Dispose();

    protected static string[] Names(ReadModelInstances page) => page.Instances.Select(instance =>
        (string)((IDictionary<string, object?>)instance)["name"]!).ToArray();
}
