// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_delete_drains_source_resolution : given.a_manager_with_blocked_source_resolution
{
    Task Because() => DeleteDuring(_manager.EnsureSubscribedIn("first"));

    [Fact] void should_wait_for_the_admitted_source_resolution() => _teardownWaited.ShouldBeTrue();
    [Fact] void should_finish_deleted() => _exists.ShouldBeFalse();
    [Fact] void should_leave_no_subscription() => _subscribed.ShouldBeFalse();
    [Fact] void should_only_subscribe_before_teardown() => _operations.ShouldEqual(["subscribe", "unsubscribe", "remove", "unsubscribe", "remove", "delete"]);
}
