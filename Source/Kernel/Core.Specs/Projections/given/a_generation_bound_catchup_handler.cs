// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler.given;

/// <summary>
/// A catch-up handler whose observer resolves to a bound later generation with an admitted owner grant.
/// </summary>
public class a_generation_bound_catchup_handler : Specification
{
    protected ProjectionCatchupHandler _handler;
    protected ObserverDetails _details;
    protected IGenerationProjectionResolver _generations;
    protected IGenerationPipelines _generationPipelines;
    protected IProjectionPipelineManager _legacyPipelines;
    protected Engine.IProjectionsManager _projections;
    protected IProjectionPipeline _generationPipeline;
    protected Engine.IProjection _generationProjection;
    protected GenerationExecution _execution;
    protected GenerationSinkContext _context;
    protected GenerationBulkWindowStore _windowStore;

    void Establish()
    {
        _details = new(new("projection", "store", "namespace", EventSequenceId.Log), ObserverType.Projection);
        _generationProjection = Substitute.For<Engine.IProjection>();
        _execution = GenerationExecutions.Create(_generationProjection);
        _context = GenerationExecutions.Context(_execution);

        _generations = Substitute.For<IGenerationProjectionResolver>();
        _generations.Resolve(_details.Key.EventStore, _details.Key.Namespace, _details.Key.EventSequenceId, _details.Key.ObserverId).Returns(_execution);
        _generations.Admit(_details.Key.EventStore, _execution).Returns(_context);

        _generationPipeline = Substitute.For<IProjectionPipeline>();
        _generationPipeline.EndBulk().Returns(Task.FromResult<IEnumerable<Storage.Sinks.FailedPartition>>([]));
        _generationPipelines = Substitute.For<IGenerationPipelines>();
        _generationPipelines
            .GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>(), Arg.Any<GenerationSinkContext>())
            .Returns(_generationPipeline);

        _projections = Substitute.For<Engine.IProjectionsManager>();
        _legacyPipelines = Substitute.For<IProjectionPipelineManager>();
        _windowStore = new();
        _handler = CreateHandler();
    }

    /// <summary>Creates another transient handler sharing the silo's window store.</summary>
    /// <returns>The handler.</returns>
    protected ProjectionCatchupHandler CreateHandler() => new(
        _projections,
        new ForwardingPipelineManager(_legacyPipelines, _generationPipelines),
        Substitute.For<IGrainFactory>(),
        NullLogger<ProjectionCatchupHandler>.Instance,
        _generations,
        _windowStore);
}
