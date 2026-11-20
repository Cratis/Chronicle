// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    static readonly TimeSpan _longestLastSeenRefresh = TimeSpan.FromHours(1);

    TimeSpan _unseenRemovalAfter = Observers.DefaultUnseenRemovalAfter;
    DateTimeOffset _lastSeenRecordedAt;

    /// <summary>
    /// Gets whether a client holds this observer's subscription right now.
    /// </summary>
    bool IsHeldByClient => _subscription.IsSubscribed && _subscription.Targets.Count > 0;

    /// <summary>
    /// Gets how often the watchdog refreshes the time while a client holds the subscription: often enough that a subscription
    /// held for weeks never looks unseen, rarely enough to stay a few writes per observer per day.
    /// </summary>
    TimeSpan LastSeenRefreshInterval => TimeSpan.FromTicks(Math.Min(_longestLastSeenRefresh.Ticks, _unseenRemovalAfter.Ticks / 4));

    /// <summary>
    /// Records, for the removal of observers no client has used for a long time, that a client holds the subscription.
    /// </summary>
    /// <param name="force">True to write regardless of when the time was last written, which a subscription and the last
    /// client leaving do; false to write at most once per refresh interval, which the watchdog does.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Only client reactors and reducers are ever candidates for removal. The time is recorded whether or not the removal is
    /// turned on, so that turning it on later does not start every clock from zero.
    /// A failure to write is logged and never fails the subscription: the worst outcome is a stale time, which the removal
    /// sweep's own guard (a subscribed observer is never removed) covers.
    /// </remarks>
    async Task RecordSeenByClient(bool force)
    {
        if (_removed || !_observerExists)
        {
            return;
        }

        var definition = observerDefinition.State;
        if (definition.Owner != ObserverOwner.Client || definition.Type is not (ObserverType.Reactor or ObserverType.Reducer))
        {
            return;
        }

        var now = _clock.GetUtcNow();
        if (!force && _lastSeenRecordedAt != default && now - _lastSeenRecordedAt < LastSeenRefreshInterval)
        {
            return;
        }

        try
        {
            await ObserverStateStorage.SaveLastSeenAt(_observerId, now, _observerKey.EventSequenceId);
            _lastSeenRecordedAt = now;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.FailedToRecordLastSeen(ex);
        }
    }
}
