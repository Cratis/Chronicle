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
        _harness = CreateHarness();
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
            await Write(name, name);
        }
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

    /// <summary>
    /// Sets the name of the instance with the given key, creating the instance when it does not exist.
    /// </summary>
    /// <param name="key">The key of the instance.</param>
    /// <param name="name">The name to set.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task Write(string key, string name)
    {
        var state = new ExpandoObject();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(state);
        changeset.Changes.Returns((Change[])[
            new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference(new PropertyPath("name"), null, name)])
        ]);
        await _sink.ApplyChanges(new Key(key, ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.First);
    }

    protected static string[] Names(ReadModelInstances page) => Names(page.Instances);

    protected static string[] Names(IEnumerable<ExpandoObject> instances) => instances.Select(instance =>
        (string)((IDictionary<string, object?>)instance)["name"]!).ToArray();
}
