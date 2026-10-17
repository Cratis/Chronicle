// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_another_pass_is_pending : given.a_captures_manager
{
    readonly TaskCompletionSource _subscription = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _joinedPendingPass;
    bool _otherNamespaceCompleted;

    void Establish()
    {
        StartedState(_events);
        _subscriptions.Subscribe(_eventStore, "pending-tenant", Arg.Any<CaptureDefinition>()).Returns(_subscription.Task);
        _subscriptions.Subscribe(_eventStore, "other-tenant", Arg.Any<CaptureDefinition>()).Returns(_ =>
        {
            _observer.IsSubscribed().Returns(true);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        var first = _manager.EnsureSubscribedIn("pending-tenant");
        var second = _manager.EnsureSubscribedIn("pending-tenant");
        _joinedPendingPass = ReferenceEquals(first, second) && !second.IsCompleted;
        var other = _manager.EnsureSubscribedIn("other-tenant");
        _otherNamespaceCompleted = other.IsCompletedSuccessfully;
        _subscription.TrySetResult();
        await Task.WhenAll(first, second, other).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_join_the_pending_subscription() => _joinedPendingPass.ShouldBeTrue();
    [Fact] void should_allow_another_namespace_to_subscribe() => _otherNamespaceCompleted.ShouldBeTrue();
    [Fact] async Task should_establish_the_other_namespace_subscription() => await _subscriptions.Received(1).Subscribe(_eventStore, "other-tenant", _eventsDefinition);
    [Fact] async Task should_only_start_one_subscription_in_the_pending_namespace() => await _subscriptions.Received(1).Subscribe(_eventStore, "pending-tenant", _eventsDefinition);
}
