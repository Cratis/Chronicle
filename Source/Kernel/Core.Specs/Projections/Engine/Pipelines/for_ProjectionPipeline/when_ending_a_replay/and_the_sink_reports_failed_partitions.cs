// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_ending_a_replay;

/// <summary>
/// Failed partitions from the final flush are handed back for the observer to record and retry; they do not keep any
/// part of the pipeline in replay.
/// </summary>
public class and_the_sink_reports_failed_partitions : Specification
{
    readonly FailedPartition _failedPartition = new(new Key("partition", ArrayIndexers.NoIndexers), 42UL);
    ISink _sink;
    IChangesetStorage _changesetStorage;
    IReplayScopedCache _replayScopedCache;
    ReplayContext _context;
    ProjectionPipeline _pipeline;
    FailedPartition[] _result;

    void Establish()
    {
        var readModel = new ReadModelDefinition(
            "id",
            "things",
            "Things",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>(),
            []);
        var projection = Substitute.For<IProjection>();
        projection.ReadModel.Returns(readModel);

        _context = new(new ReadModelType("id", ReadModelGeneration.First), "things", "things-revert", DateTimeOffset.UtcNow);
        _sink = Substitute.For<ISink>();
        _sink.EndReplay(_context).Returns(Task.FromResult<IEnumerable<FailedPartition>>([_failedPartition]));
        _changesetStorage = Substitute.For<IChangesetStorage>();
        _replayScopedCache = Substitute.For<IReplayScopedCache>();

        _pipeline = new ProjectionPipeline(
            projection,
            _sink,
            _changesetStorage,
            Substitute.For<IObjectComparer>(),
            [],
            new ProjectionHandleLock(),
            _replayScopedCache,
            NullLogger<ProjectionPipeline>.Instance);
    }

    async Task Because() => _result = (await _pipeline.EndReplay(_context)).ToArray();

    [Fact] void should_return_the_failed_partitions() => _result.ShouldContainOnly(_failedPartition);
    [Fact] void should_end_the_replay_for_the_changeset_storage() => _changesetStorage.Received(1).EndReplay((ReadModelContainerName)"things");
    [Fact] void should_end_the_replay_session() => _replayScopedCache.Received(1).EndReplaySession();
}
