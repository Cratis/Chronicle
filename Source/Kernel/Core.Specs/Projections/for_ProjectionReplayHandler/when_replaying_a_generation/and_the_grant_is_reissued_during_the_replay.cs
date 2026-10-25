// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_replaying_a_generation;

public class and_the_grant_is_reissued_during_the_replay : given.a_generation_bound_replay_handler
{
    GenerationSinkContext _reissued;

    async Task Establish()
    {
        _generationPipeline.FlushBulk().Returns(Task.FromResult(new BulkFlushResult(null, [])));
        await _handler.BeginReplayFor(_observerDetails);
        _reissued = GenerationExecutions.Context(_execution, 2);
        _generations.Admit(_observerDetails.Key.EventStore, _execution).Returns(_reissued);
    }

    async Task Because()
    {
        await _handler.FlushFor(_observerDetails);
        await _handler.EndReplayFor(_observerDetails);
    }

    [Fact] void should_not_switch_to_the_reissued_grant() => _generationPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>(), _reissued);
    [Fact] void should_flush_and_end_on_the_grant_the_replay_began_with() => _generationPipelines.Received(3).GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _generationProjection, _context);
    [Fact] void should_resolve_the_generation_once() => _generations.Received(1).Resolve(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _observerDetails.Key.EventSequenceId, _observerDetails.Key.ObserverId);
}
