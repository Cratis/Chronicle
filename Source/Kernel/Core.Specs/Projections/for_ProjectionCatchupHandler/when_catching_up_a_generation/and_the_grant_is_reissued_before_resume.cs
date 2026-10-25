// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler.when_catching_up_a_generation;

public class and_the_grant_is_reissued_before_resume : given.a_generation_bound_catchup_handler
{
    GenerationSinkContext _reissued;

    async Task Establish()
    {
        _generationPipeline.FlushBulk().Returns(Task.FromResult(new BulkFlushResult(null, [])));
        await _handler.BeginCatchupFor(_details);
        _reissued = GenerationExecutions.Context(_execution, 2);
        _generations.Admit(_details.Key.EventStore, _execution).Returns(_reissued);
    }

    async Task Because()
    {
        var nextHandler = CreateHandler();
        await nextHandler.ResumeCatchupFor(_details);
        await nextHandler.FlushFor(_details);
        await nextHandler.EndCatchupFor(_details);
    }

    [Fact] void should_keep_the_begin_grant() => _generationPipelines.Received(4).GetFor(_details.Key.EventStore, _details.Key.Namespace, _generationProjection, _context);
    [Fact] void should_not_switch_grants() => _generationPipelines.DidNotReceive().GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Engine.IProjection>(), _reissued);
    [Fact] void should_resolve_only_at_begin() => _generations.Received(1).Resolve(_details.Key.EventStore, _details.Key.Namespace, _details.Key.EventSequenceId, _details.Key.ObserverId);
}
