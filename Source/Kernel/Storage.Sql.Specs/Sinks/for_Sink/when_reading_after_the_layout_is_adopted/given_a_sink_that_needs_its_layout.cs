// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_after_the_layout_is_adopted;

/// <summary>
/// A sink whose identifier is spelled other than <c language="csharp">Id</c> or <c language="csharp">id</c>, so it resolves its table layout, and a harness that lets a
/// specification pause or fail the first table open, which is the write path adopting the layout.
/// </summary>
public class given_a_sink_that_needs_its_layout : Specification
{
    protected SqlSinkHarness _harness;
    protected Sink _sink;
    protected TaskCompletionSource _adoptionReached;
    protected TaskCompletionSource _adoptionGate;
    int _opens;

    protected virtual Task OnTableOpening(int ordinal) => Task.CompletedTask;

    void Establish()
    {
        _adoptionReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _adoptionGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _harness = new SqlSinkHarness { BeforeOpeningTable = _ => OnTableOpening(Interlocked.Increment(ref _opens)) };
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "ID":{"type":"string"},
              "title":{"type":"string"},
              "books":{"type":"array","items":{"type":"object","properties":{"isbn":{"type":"string"}}}}
            }}
            """);
        var definition = new ReadModelDefinition("shelf", "shelves", "Shelf", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "shelf-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        _sink = (Sink)_harness.CreateSink(definition);
    }

    /// <summary>
    /// Runs a write-path operation that adopts the layout.
    /// </summary>
    /// <returns>The task of the operation.</returns>
    protected Task AdoptLayout() => _sink.TryFindRootKeyByChildValue(new PropertyPath("books.isbn"), "x");

    void Destroy() => _harness.Dispose();
}
