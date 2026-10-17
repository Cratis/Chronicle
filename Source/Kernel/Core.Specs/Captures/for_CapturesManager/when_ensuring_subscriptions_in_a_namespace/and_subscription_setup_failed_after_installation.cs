// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_subscription_setup_failed_after_installation : given.a_captures_manager
{
    bool _established;

    void Establish()
    {
        StartedState(_events);
        _observer.IsSubscribed().Returns(true);
        _observer.IsSubscriptionEstablished().Returns(_ => _established);
        _subscriptions.Recover(_eventStore, "tenant", Arg.Any<CaptureDefinition>()).Returns(_ =>
        {
            _established = true;
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _manager.EnsureSubscribedIn("tenant");
        await _manager.EnsureSubscribedIn("tenant");
    }

    [Fact] async Task should_recover_the_installed_but_incomplete_subscription_once() => await _subscriptions.Received(1).Recover(_eventStore, "tenant", _eventsDefinition);
    [Fact] void should_establish_the_subscription() => _established.ShouldBeTrue();
}
