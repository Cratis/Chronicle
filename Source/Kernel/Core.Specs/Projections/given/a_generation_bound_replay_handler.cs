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
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.given;

/// <summary>
/// A replay handler whose observer resolves to a bound later generation with an admitted owner grant.
/// </summary>
public class a_generation_bound_replay_handler : a_projection_replay_handler
{
    protected IGenerationProjectionResolver _generations;
    protected IGenerationPipelines _generationPipelines;
    protected IProjectionPipeline _generationPipeline;
    protected Engine.IProjection _generationProjection;
    protected GenerationExecution _execution;
    protected GenerationSinkContext _context;

    void Establish()
    {
        _generationProjection = Substitute.For<Engine.IProjection>();
        _execution = GenerationExecutions.Create(_generationProjection);
        _context = GenerationExecutions.Context(_execution);

        _generations = Substitute.For<IGenerationProjectionResolver>();
        _generations
            .Resolve(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _observerDetails.Key.EventSequenceId, _observerDetails.Key.ObserverId)
            .Returns(_execution);
        _generations.Admit(_observerDetails.Key.EventStore, _execution).Returns(_context);

        _generationPipeline = Substitute.For<IProjectionPipeline>();
        _generationPipeline.EndBulk().Returns(Task.FromResult<IEnumerable<Storage.Sinks.FailedPartition>>([]));
        _generationPipelines = Substitute.For<IGenerationPipelines>();
        _generationPipelines
            .GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>(), Arg.Any<GenerationSinkContext>())
            .Returns(_generationPipeline);

        _handler = new ProjectionReplayHandler(
            _projections,
            _grainFactory,
            _storage,
            new ForwardingPipelineManager(_projectionPipelineManager, _generationPipelines),
            Options.Create(new Configuration.ChronicleOptions()),
            NullLogger<ProjectionReplayHandler>.Instance,
            _generations);
    }

    protected void Unbind() => _generations
        .Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>())
        .Returns((GenerationExecution?)null);
}
