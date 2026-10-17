// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_capture_is_stopped_during_observer_checks : given.a_captures_manager
{
    readonly TaskCompletionSource<bool> _subscriptionCheck = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _checking = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _stopDrained;

    void Establish()
    {
        StartedState(_events);
        _observer.IsSubscriptionEstablished().Returns(_ =>
        {
            _checking.TrySetResult();
            return _subscriptionCheck.Task;
        });
    }

    async Task Because()
    {
        var pass = _manager.EnsureSubscribedIn("tenant");
        await _checking.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var stop = _manager.Stop(_events.Id);
        _stopDrained = !stop.IsCompleted;
        _subscriptionCheck.TrySetResult(false);
        await Task.WhenAll(pass, stop).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _manager.EnsureSubscribedIn("tenant");
    }

    [Fact] async Task should_keep_the_capture_stopped() => (await _captures.Get(_events.Id)).Status.ShouldEqual(CaptureStatus.Stopped);
    [Fact] void should_drain_the_in_flight_operation() => _stopDrained.ShouldBeTrue();
    [Fact] async Task should_unsubscribe_after_the_in_flight_operation() => await _subscriptions.Received(1).Unsubscribe(_eventStore, _eventsDefinition);
    [Fact] async Task should_not_admit_another_subscription_after_stop() => await _subscriptions.Received(1).Recover(_eventStore, "tenant", Arg.Any<CaptureDefinition>());
}
