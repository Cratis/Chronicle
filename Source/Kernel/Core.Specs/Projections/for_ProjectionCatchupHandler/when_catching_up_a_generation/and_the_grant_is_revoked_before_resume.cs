// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Monads;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler.when_catching_up_a_generation;

public class and_the_grant_is_revoked_before_resume : given.a_generation_bound_catchup_handler
{
    Result<ICanHandleCatchupForObserver.Error> _result;

    async Task Establish()
    {
        await _handler.BeginCatchupFor(_details);
        _generations.Admit(_details.Key.EventStore, _execution).ThrowsAsync(new GenerationWriterGrantNotAdmitted());
    }

    async Task Because() => _result = await CreateHandler().ResumeCatchupFor(_details);

    [Fact] void should_fail_closed() => _result.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_not_resume() => _generationPipeline.DidNotReceive().ResumeBulk();
    [Fact] void should_not_build_another_pipeline() => _generationPipelines.Received(1).GetFor(_details.Key.EventStore, _details.Key.Namespace, _generationProjection, _context);
    [Fact] void should_not_fall_back() => _legacyPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
}
