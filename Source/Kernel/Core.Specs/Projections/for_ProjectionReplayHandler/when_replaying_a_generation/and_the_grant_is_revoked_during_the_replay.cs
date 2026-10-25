// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Generations;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_replaying_a_generation;

public class and_the_grant_is_revoked_during_the_replay : given.a_generation_bound_replay_handler
{
    Exception _error;

    async Task Establish()
    {
        await _handler.BeginReplayFor(_observerDetails);
        _generations.Admit(_observerDetails.Key.EventStore, _execution).ThrowsAsync(new GenerationWriterGrantNotAdmitted());
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.FlushFor(_observerDetails));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<GenerationWriterGrantNotAdmitted>();
    [Fact] void should_not_flush_the_bulk_load() => _generationPipeline.DidNotReceive().FlushBulk();
    [Fact] void should_not_fall_back_to_a_legacy_pipeline() => _projectionPipelineManager.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
}
