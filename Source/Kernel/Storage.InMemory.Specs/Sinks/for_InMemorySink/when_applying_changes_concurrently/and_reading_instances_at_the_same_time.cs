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

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes_concurrently;

/// <summary>
/// REPRO: the sink is written by projection threads while queries and observers read it. Before the
/// backing store was synchronized and snapshots materialized, a query enumerating the deferred
/// Skip/Take over the live dictionary raced an in-flight mutation and threw
/// InvalidOperationException — an intermittent harness flake. This hammers writes and reads in
/// parallel for enough iterations to have reproduced the old flake reliably.
/// </summary>
public class and_reading_instances_at_the_same_time : Specification
{
    const int WriterCount = 8;
    const int ReaderCount = 8;
    const int Iterations = 1000;

    InMemorySink _sink;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Exception _error;

    void Establish()
    {
        _sink = new InMemorySink(CreateReadModelDefinition(), new TypeFormats());

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(new ExpandoObject());
        _changeset.Changes.Returns([]);
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        using var subscription = _sink.ObserveInstances(skip: 0, take: 500).Subscribe(instances => _ = instances.Count());

        var writers = Enumerable.Range(0, WriterCount).Select(writer => Task.Run(async () =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var key = new Key($"writer-{writer}-item-{i}", ArrayIndexers.NoIndexers);
                await _sink.ApplyChanges(key, _changeset, (ulong)i);
                if (i % 3 == 0)
                {
                    _sink.RemoveAnyExisting(key);
                }
            }
        }));

        var readers = Enumerable.Range(0, ReaderCount).Select(reader => Task.Run(async () =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var result = await _sink.GetInstances(skip: 0, take: 500);
                _ = result.Instances.Count();
            }
        }));

        await Task.WhenAll(writers.Concat(readers));
    });

    [Fact] void should_not_throw_while_reading_and_writing_concurrently() => _error.ShouldBeNull();

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestReadModel",
            "TestReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            []);
}
