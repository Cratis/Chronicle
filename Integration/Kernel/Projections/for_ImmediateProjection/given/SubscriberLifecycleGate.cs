// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Orders a subscriber's unsubscribe, the definition notification and its resubscribe. The gate is re-armable because
/// every context in the collection shares the silo it is registered in.
/// </summary>
public class SubscriberLifecycleGate
{
    TaskCompletionSource _unsubscribeEntered = NewSignal();
    TaskCompletionSource _releaseUnsubscribe = NewSignal();
    TaskCompletionSource _notificationSent = NewSignal();
    TaskCompletionSource _resubscribed = NewSignal();
    int _unsubscribeArmed;
    int _resubscribeArmed;
    int _order;
    int _unsubscribeCompletedOrder;
    int _resubscribeCompletedOrder;

    /// <summary>
    /// Gets the signal that the armed unsubscribe has been dispatched.
    /// </summary>
    public Task UnsubscribeEntered => _unsubscribeEntered.Task;

    /// <summary>
    /// Gets the signal that the definition notification has been dispatched to the subscriber.
    /// </summary>
    public Task NotificationSent => _notificationSent.Task;

    /// <summary>
    /// Gets the signal that the subscriber has subscribed again after the notification.
    /// </summary>
    public Task Resubscribed => _resubscribed.Task;

    /// <summary>
    /// Gets whether the armed unsubscribe must be lost instead of delivered.
    /// </summary>
    public bool DropUnsubscribe { get; private set; }

    /// <summary>
    /// Gets the order in which the held unsubscribe completed, or zero when it did not.
    /// </summary>
    public int UnsubscribeCompletedOrder => Volatile.Read(ref _unsubscribeCompletedOrder);

    /// <summary>
    /// Gets the order in which the resubscribe completed, or zero when it did not.
    /// </summary>
    public int ResubscribeCompletedOrder => Volatile.Read(ref _resubscribeCompletedOrder);

    /// <summary>
    /// Arms the gate for the next unsubscribe, replacing all signals and counters.
    /// </summary>
    /// <param name="dropUnsubscribe">Whether the unsubscribe is lost so the subscriber deactivates still subscribed.</param>
    public void Arm(bool dropUnsubscribe)
    {
        DropUnsubscribe = dropUnsubscribe;
        _unsubscribeEntered = NewSignal();
        _releaseUnsubscribe = NewSignal();
        _notificationSent = NewSignal();
        _resubscribed = NewSignal();
        Volatile.Write(ref _order, 0);
        Volatile.Write(ref _unsubscribeCompletedOrder, 0);
        Volatile.Write(ref _resubscribeCompletedOrder, 0);
        Volatile.Write(ref _resubscribeArmed, 1);
        Volatile.Write(ref _unsubscribeArmed, 1);
    }

    /// <summary>
    /// Gets the next position in the shared ordering.
    /// </summary>
    /// <returns>The position, starting at one.</returns>
    public int NextOrder() => Interlocked.Increment(ref _order);

    /// <summary>
    /// Attempts to select the next unsubscribe.
    /// </summary>
    /// <returns>Whether this invocation was selected.</returns>
    public bool TryEnterUnsubscribe()
    {
        if (Interlocked.CompareExchange(ref _unsubscribeArmed, 0, 1) != 1)
        {
            return false;
        }

        _unsubscribeEntered.TrySetResult();
        return true;
    }

    /// <summary>
    /// Waits until the test releases the held unsubscribe.
    /// </summary>
    /// <returns>The bounded wait.</returns>
    public Task WaitForUnsubscribeRelease() => _releaseUnsubscribe.Task.WaitAsync(TimeSpan.FromSeconds(30));

    /// <summary>
    /// Releases the held unsubscribe, including when the test fails.
    /// </summary>
    public void ReleaseUnsubscribe() => _releaseUnsubscribe.TrySetResult();

    /// <summary>
    /// Records the completion of the held unsubscribe.
    /// </summary>
    public void UnsubscribeCompleted() => Interlocked.CompareExchange(ref _unsubscribeCompletedOrder, NextOrder(), 0);

    /// <summary>
    /// Records that the definition notification was dispatched while the gate is armed.
    /// </summary>
    public void NotificationWasSent()
    {
        if (_unsubscribeEntered.Task.IsCompleted)
        {
            _notificationSent.TrySetResult();
        }
    }

    /// <summary>
    /// Attempts to select the first subscribe after the notification.
    /// </summary>
    /// <returns>Whether this invocation was selected.</returns>
    public bool TryEnterResubscribe() =>
        _notificationSent.Task.IsCompleted && Interlocked.CompareExchange(ref _resubscribeArmed, 0, 1) == 1;

    /// <summary>
    /// Records the completion of the resubscribe.
    /// </summary>
    public void ResubscribeCompleted()
    {
        Interlocked.CompareExchange(ref _resubscribeCompletedOrder, NextOrder(), 0);
        _resubscribed.TrySetResult();
    }

    static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
