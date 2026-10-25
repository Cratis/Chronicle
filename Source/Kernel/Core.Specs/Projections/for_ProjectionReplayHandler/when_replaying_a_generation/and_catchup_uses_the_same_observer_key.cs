// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_replaying_a_generation;

public class and_catchup_uses_the_same_observer_key : given.a_generation_bound_replay_handler
{
    ProjectionCatchupHandler _catchup;
    GenerationSinkContext _catchupContext;

    async Task Establish()
    {
        _generationPipeline.FlushBulk().Returns(Task.FromResult(new BulkFlushResult(null, [])));
        await _handler.BeginReplayFor(_observerDetails);
        _catchupContext = GenerationExecutions.Context(_execution, 2);
        _generations.Admit(_observerDetails.Key.EventStore, _execution).Returns(_catchupContext);
        _catchup = new(
            _projections,
            new ForwardingPipelineManager(_projectionPipelineManager, _generationPipelines),
            _grainFactory,
            NullLogger<ProjectionCatchupHandler>.Instance,
            _generations,
            _windowStore);
        await _catchup.BeginCatchupFor(_observerDetails);
    }

    async Task Because()
    {
        await CreateHandler().FlushFor(_observerDetails);
        await _catchup.FlushFor(_observerDetails);
    }

    [Fact] void should_keep_the_replay_window() => _generationPipelines.Received(2).GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _generationProjection, _context);
    [Fact] void should_keep_a_separate_catchup_window() => _generationPipelines.Received(2).GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _generationProjection, _catchupContext);
}
