// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_observer_refuses_the_subscription : given.a_captures_manager
{
    Exception _exception;

    void Establish() => StartedState(_events);

    async Task Because()
    {
        _exception = await Catch.Exception(() => _manager.EnsureSubscribedIn("tenant"));
        await _manager.EnsureSubscribedIn("tenant");
    }

    [Fact] void should_not_fail_namespace_creation() => _exception.ShouldBeNull();
    [Fact] async Task should_retry_on_the_next_trigger() => await _subscriptions.Received(2).Recover(_eventStore, "tenant", _eventsDefinition);
}
