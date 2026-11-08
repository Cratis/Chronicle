// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences;

public partial class AppendedEventsQueue
{
    readonly Dictionary<string, HashSet<EventTypeId>> _holds = new(StringComparer.Ordinal);
    readonly Dictionary<ObserverKey, AppendedEventsQueueObserverSubscription> _heldSubscriptions = [];

    /// <inheritdoc/>
    public Task HoldBack(string holdId, IEnumerable<EventTypeId> eventTypes)
    {
        lock (_subscriptionsLock)
        {
            if (_isResetting || _isDisposed)
            {
                return Task.CompletedTask;
            }

            var types = eventTypes.ToHashSet();
            if (_holds.TryGetValue(holdId, out var existing))
            {
                existing.UnionWith(types);
            }
            else
            {
                _holds[holdId] = types;
            }

            foreach (var subscription in _subscriptions.ToArray())
            {
                if (!IsHeld(subscription)) continue;
                _subscriptions.Remove(subscription);
                CloseLane(subscription.ObserverKey);
                _heldSubscriptions[subscription.ObserverKey] = subscription;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ReleaseHold(string holdId)
    {
        List<ObserverKey> released = [];
        lock (_subscriptionsLock)
        {
            if (!_holds.Remove(holdId))
            {
                return Task.CompletedTask;
            }

            foreach (var (observerKey, subscription) in _heldSubscriptions.ToArray())
            {
                if (IsHeld(subscription)) continue;
                _heldSubscriptions.Remove(observerKey);
                released.Add(observerKey);
            }
        }

        // The observers left live delivery with their cursors where they were. One catch-up each reads everything they
        // were held back from, the import's events and any ordinary append that interleaved with it, in order.
        foreach (var observerKey in released)
        {
            StartCatchupTrigger(observerKey);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a subscription, or keeps it held back when a hold covers it. Must be called holding the subscriptions lock.
    /// </summary>
    /// <param name="subscription">The <see cref="AppendedEventsQueueObserverSubscription"/>.</param>
    void AddOrHold(AppendedEventsQueueObserverSubscription subscription)
    {
        if (IsHeld(subscription))
        {
            _heldSubscriptions[subscription.ObserverKey] = subscription;
            CloseLane(subscription.ObserverKey);
            return;
        }

        _heldSubscriptions.Remove(subscription.ObserverKey);
        _subscriptions.Add(subscription);
    }

    bool IsHeld(AppendedEventsQueueObserverSubscription subscription) =>
        _holds.Values.Any(types => subscription.AllEventTypes || subscription.EventTypeIds.Any(types.Contains));
}
