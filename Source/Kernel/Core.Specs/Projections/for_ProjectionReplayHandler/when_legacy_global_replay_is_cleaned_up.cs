// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler;

public class when_legacy_global_replay_is_cleaned_up : given.a_projection_replay_handler_with_projection
{
    Exception _flushError;

    void Establish()
    {
        _projection.Scope.Returns(ProjectionScope.Global);
        _projections.TryGet(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _observerDetails.Key.ObserverId, out Arg.Any<Engine.IProjection>()).Returns(call =>
        {
            call[3] = _projection;
            return true;
        });
    }

    async Task Because() => _flushError = await Catch.Exception(() => _handler.FlushReplayFor(_observerDetails));

    [Fact] void should_refuse_to_flush_an_uncoordinated_rebuild() => _flushError.Message.ShouldEqual(GlobalProjectionReplay.RefusalReason);
    [Fact] void should_not_flush_or_promote_the_shared_sink() => _projectionPipeline.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_change_source_namespace_contexts() => _replayContexts.ReceivedCalls().ShouldBeEmpty();
}
