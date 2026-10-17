// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_subscription_was_lost : given.a_captures_manager
{
    bool _subscribed;

    async Task Establish()
    {
        StartedState(_events);
        _observer.IsSubscribed().Returns(_ => _subscribed);
        _subscriptions.Subscribe(_eventStore, "tenant", Arg.Any<CaptureDefinition>()).Returns(_ =>
        {
            _subscribed = true;
            return Task.CompletedTask;
        });
        await _manager.EnsureSubscribedIn("tenant");
        await _manager.EnsureSubscribedIn("tenant");
        _subscriptions.ClearReceivedCalls();
        _subscribed = false;
    }

    Task Because() => _manager.EnsureSubscribedIn("tenant");

    [Fact] void should_restore_the_observers_actual_subscription() => _subscribed.ShouldBeTrue();
    [Fact] async Task should_subscribe_in_only_the_requested_namespace() => await _subscriptions.Received(1).Subscribe(_eventStore, "tenant", _eventsDefinition);
    [Fact] async Task should_not_resubscribe_every_namespace() => await _subscriptions.DidNotReceive().Subscribe(_eventStore, Arg.Any<CaptureDefinition>());
}
