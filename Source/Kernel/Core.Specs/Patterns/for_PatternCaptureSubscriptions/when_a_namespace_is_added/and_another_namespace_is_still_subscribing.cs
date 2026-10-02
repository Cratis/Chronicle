// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriptions.when_a_namespace_is_added;

public class and_another_namespace_is_still_subscribing : given.a_namespace_subscription
{
    readonly TaskCompletionSource _releaseSubscription = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _subscriptionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    NamespaceAdded _other;
    bool _notificationReturnedBeforeSubscribing;
    bool _otherSubscribedBeforeTheFirstCompleted;

    void Establish()
    {
        _other = new NamespaceAdded(_added.EventStore, "other-namespace");
        _capture.Subscribe(_added.EventStore, _added.Namespace).Returns(_ =>
        {
            _subscriptionStarted.TrySetResult();
            return _releaseSubscription.Task;
        });
        _capture.Subscribe(_other.EventStore, _other.Namespace).Returns(_ =>
        {
            _otherSubscribedBeforeTheFirstCompleted = !_releaseSubscription.Task.IsCompleted;
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _onNamespaceAdded(_added);
        _notificationReturnedBeforeSubscribing = !_subscriptionStarted.Task.IsCompleted;
        var firstTurn = _silo.TimerRegistry.FireAllAsync();
        try
        {
            await _subscriptionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _onNamespaceAdded(_added);
            await _onNamespaceAdded(_other);
            await _silo.TimerRegistry.FireAllAsync().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _releaseSubscription.TrySetResult();
            await firstTurn;
        }
    }

    [Fact] void should_release_the_broadcast_before_subscribing() => _notificationReturnedBeforeSubscribing.ShouldBeTrue();
    [Fact] void should_not_hold_up_the_other_namespace() => _otherSubscribedBeforeTheFirstCompleted.ShouldBeTrue();
    [Fact] async Task should_coalesce_duplicate_notifications_while_subscribing() => await _capture.Received(1).Subscribe(_added.EventStore, _added.Namespace);
    [Fact] void should_dispose_both_notification_timers() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
