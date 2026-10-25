// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_replaying_a_generation;

public class and_the_owner_grant_is_not_admitted : given.a_generation_bound_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish() => _generations.Admit(_observerDetails.Key.EventStore, _execution).ThrowsAsync(new GenerationWriterGrantNotAdmitted());

    async Task Because() => _result = await _handler.BeginReplayFor(_observerDetails);

    [Fact] void should_report_an_error() => _result.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_not_build_a_generation_pipeline() => _generationPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>(), Arg.Any<GenerationSinkContext>());
    [Fact] void should_not_fall_back_to_the_current_projection() => _projections.DidNotReceive().GetCurrent(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>());
}
