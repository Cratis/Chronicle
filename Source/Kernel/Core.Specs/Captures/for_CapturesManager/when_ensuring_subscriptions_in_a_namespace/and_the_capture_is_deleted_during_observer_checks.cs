// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_capture_is_deleted_during_observer_checks : given.a_captures_manager
{
    readonly TaskCompletionSource<bool> _subscriptionCheck = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _checking = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _deleteDrained;

    void Establish()
    {
        StartedState(_events);
        _observer.IsSubscriptionEstablished().Returns(_ =>
        {
            _checking.TrySetResult();
            return _subscriptionCheck.Task;
        });
        _captures.Delete(_events.Id).Returns(_ =>
        {
            _captures.Has(_events.Id).Returns(false);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        var pass = _manager.EnsureSubscribedIn("tenant");
        await _checking.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var delete = _manager.Delete(_events.Id);
        _deleteDrained = !delete.IsCompleted;
        _subscriptionCheck.TrySetResult(false);
        await Task.WhenAll(pass, delete).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _manager.EnsureSubscribedIn("tenant");
    }

    [Fact] async Task should_keep_the_capture_deleted() => (await _captures.Has(_events.Id)).ShouldBeFalse();
    [Fact] void should_drain_the_in_flight_operation() => _deleteDrained.ShouldBeTrue();
    [Fact] async Task should_remove_after_the_in_flight_operation() => await _subscriptions.Received(1).Remove(_eventStore, _eventsDefinition);
    [Fact] async Task should_not_admit_another_subscription_after_delete() => await _subscriptions.Received(1).Recover(_eventStore, "tenant", Arg.Any<CaptureDefinition>());
}
