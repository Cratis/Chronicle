// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_the_resolved_future_has_not_been_saved : given.a_first_level_child_future
{
    void Establish() => SetFutureKey("root-key");

    async Task Because() => await ProcessRoot(RootWith());

    [Fact] void should_keep_the_future_in_storage() => _projectionFutures.DidNotReceive().ResolveFuture(_future.Id);
    [Fact] void should_queue_only_one_write_for_the_future() => _result!.PendingFutureSaves.Count().ShouldEqual(1);
    [Fact] void should_keep_the_tracker_pending() => _tracker.HasPending.ShouldBeTrue();
}
