// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Generations;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler.when_catching_up_a_generation;

public class and_the_grant_is_revoked_during_catchup : given.a_generation_bound_catchup_handler
{
    Exception _error;

    async Task Establish()
    {
        await _handler.BeginCatchupFor(_details);
        _generations.Admit(_details.Key.EventStore, _execution).ThrowsAsync(new GenerationWriterGrantNotAdmitted());
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.FlushFor(_details));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<GenerationWriterGrantNotAdmitted>();
    [Fact] void should_not_flush_the_bulk_load() => _generationPipeline.DidNotReceive().FlushBulk();
    [Fact] void should_not_fall_back_to_a_legacy_pipeline() => _legacyPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
}
