// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Monads;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_replaying_a_generation;

public class and_the_grant_is_revoked_before_resume : given.a_generation_bound_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    async Task Establish()
    {
        await _handler.BeginReplayFor(_observerDetails);
        _generations.Admit(_observerDetails.Key.EventStore, _execution).ThrowsAsync(new GenerationWriterGrantNotAdmitted());
    }

    async Task Because() => _result = await CreateHandler().ResumeReplayFor(_observerDetails);

    [Fact] void should_fail_closed() => _result.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_not_resume() => _generationPipeline.DidNotReceive().ResumeBulk();
    [Fact] void should_not_build_another_pipeline() => _generationPipelines.Received(1).GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _generationProjection, _context);
    [Fact] void should_not_fall_back() => _projectionPipelineManager.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
}
