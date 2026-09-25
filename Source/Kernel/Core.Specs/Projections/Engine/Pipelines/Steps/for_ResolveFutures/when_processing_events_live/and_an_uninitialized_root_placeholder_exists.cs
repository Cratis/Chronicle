// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_an_uninitialized_root_placeholder_exists : given.a_first_level_child_future
{
    bool _resolvedBeforeRoot;
    bool _pendingBeforeRoot;

    void Establish() => SetFutureKey("root-key");

    async Task Because()
    {
        await ProcessRoot(RootWith("id", "root-key", initialized: false));
        _resolvedBeforeRoot = _resolved;
        _pendingBeforeRoot = _tracker.HasPending;
        await ProcessRoot(RootWith("id", "root-key", initialized: false), initializeNow: true);
    }

    [Fact] void should_not_resolve_against_the_placeholder() => _resolvedBeforeRoot.ShouldBeFalse();
    [Fact] void should_keep_the_future_pending_until_the_parent_event() => _pendingBeforeRoot.ShouldBeTrue();
    [Fact] void should_add_the_child_after_the_parent_event_initializes_the_root() => HasChild.ShouldBeTrue();
}
