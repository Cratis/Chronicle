// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_resolved_future_has_changes : given.a_first_level_child_future
{
    ProjectionEventContext? _protectedContext;

    void Establish()
    {
        SetFutureKey("root-key");
        _futureProtection.When(_ => _.ProtectFutureChangeset(Arg.Any<IProjection>(), Arg.Any<ProjectionEventContext>()).AsTask())
            .Do(call => _protectedContext = call.Arg<ProjectionEventContext>());
    }

    async Task Because() => await ProcessRoot(RootWith("id", "root-key"));

    [Fact] async Task should_protect_the_future_changeset_once() => await _futureProtection.Received(1).ProtectFutureChangeset(Arg.Any<IProjection>(), Arg.Any<ProjectionEventContext>());
    [Fact] void should_protect_the_changeset_it_queues() => ReferenceEquals(_protectedContext?.Changeset, _result!.PendingFutureSaves.Single().Changeset).ShouldBeTrue();
    [Fact] void should_protect_the_future_event() => _protectedContext!.Event.ShouldEqual(_future.Event);
    [Fact] async Task should_protect_with_the_root_projection() => await _futureProtection.Received(1).ProtectFutureChangeset(_projection, Arg.Any<ProjectionEventContext>());
}
