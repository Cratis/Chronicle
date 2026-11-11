// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Measures production SaveChanges with no-op dependencies, including the zero-future path.
/// Dependencies do not retain call history or mutate the reused changeset during folding.
/// </summary>
[MemoryDiagnoser]
public class SaveChangesBenchmark
{
    static readonly Task<IEnumerable<FailedPartition>> _success = Task.FromResult<IEnumerable<FailedPartition>>([]);
    SaveChanges _step = null!;
    IProjection _projection = null!;
    ProjectionEventContext _context = null!;

    /// <summary>
    /// Gets or sets the number of pending futures.
    /// </summary>
    [Params(0, 1, 16)]
    public int PendingFutures { get; set; }

    /// <summary>
    /// Gets or sets whether pending writes fold into the main key or use separate keys.
    /// </summary>
    [Params(false, true)]
    public bool Fold { get; set; }

    /// <summary>
    /// Creates all state outside measurement. The same fixture compiles against main.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var sink = BenchmarkNoOpProxy.Bind<ISink>(name => name switch
        {
            "ApplyChanges" or "ApplyChangesImmediately" => _success,
            _ => throw new InvalidOperationException(name)
        });
        var storage = BenchmarkNoOpProxy.Bind<IChangesetStorage>(name => name == "Save" ? Task.CompletedTask : throw new InvalidOperationException(name));
        _step = new SaveChanges(sink, storage, false, NullLogger<SaveChanges>.Instance);
        var readModel = new ReadModelDefinition("model", "Model", "Model", ReadModelOwner.Client, ReadModelSource.Code,
            ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() }, []);
        _projection = BenchmarkNoOpProxy.Bind<IProjection>(name => name switch
        {
            "get_Identifier" => new ProjectionId("projection"),
            "get_ReadModel" => readModel,
            _ => throw new InvalidOperationException(name)
        });
        var @event = AppendedEvent.Empty();
        var state = new ExpandoObject();
        Change[] changes = [new PropertiesChanged<ExpandoObject>(state, [])];
        var changeset = BenchmarkNoOpProxy.Bind<IChangeset<AppendedEvent, ExpandoObject>>(name => name switch
        {
            "get_HasChanges" => true,
            "get_Incoming" => @event,
            "get_Changes" => changes,
            "get_CurrentState" => state,
            "Add" => null,
            _ => throw new InvalidOperationException(name)
        });
        _context = new(new Key("main-key", ArrayIndexers.NoIndexers), @event, changeset, ProjectionOperationType.None, false);
        for (var index = 0; index < PendingFutures; index++)
        {
            _context.AddPendingFutureSave(Fold ? _context.Key : new Key($"future-{index}", ArrayIndexers.NoIndexers), changeset);
        }
    }

    /// <summary>
    /// Runs the real save step; proxy dispatch overhead remains fixed across compared revisions.
    /// </summary>
    /// <returns>The resulting context.</returns>
    [Benchmark]
    public ValueTask<ProjectionEventContext> Save() => _step.Perform(_projection, _context);
}
