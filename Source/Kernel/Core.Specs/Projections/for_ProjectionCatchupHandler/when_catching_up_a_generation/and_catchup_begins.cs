// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler.when_catching_up_a_generation;

public class and_catchup_begins : given.a_generation_bound_catchup_handler
{
    bool _succeeded;

    async Task Because() => _succeeded = (await _handler.BeginCatchupFor(_details)).IsSuccess;

    [Fact] void should_succeed() => _succeeded.ShouldBeTrue();
    [Fact] void should_build_the_pipeline_with_the_admitted_context() => _generationPipelines.Received(1).GetFor(_details.Key.EventStore, _details.Key.Namespace, _generationProjection, _context);
    [Fact] void should_begin_a_bulk_load_on_the_generation_pipeline() => _generationPipeline.Received(1).BeginBulk();
    [Fact] void should_not_look_up_the_current_projection() => _projections.DidNotReceive().GetCurrent(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>());
    [Fact] void should_not_build_a_legacy_pipeline() => _legacyPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>());
}
