// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_the_current_root_has_another_key : given.a_first_level_child_future
{
    void Establish() => SetFutureKey("root-key");

    async Task Because() => await ProcessRoot(RootWith(), "another-root");

    [Fact] void should_leave_the_future_pending() => _resolved.ShouldBeFalse();
    [Fact] void should_not_add_the_child_to_the_other_root() => _result!.PendingFutureSaves.ShouldBeEmpty();
}
