// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.EventSequences;

public partial class AppendedEventsQueue
{
    /// <summary>
    /// Sets which queue of which event sequence this is. Done on activation, and by specifications that construct the queue directly.
    /// </summary>
    /// <param name="eventSequenceKey">The key of the event sequence the queue belongs to.</param>
    /// <param name="queueId">The index of the queue within the event sequence.</param>
    internal void Identify(EventSequenceKey eventSequenceKey, int queueId)
    {
        _eventSequenceKey = eventSequenceKey;
        _queueId = queueId;
    }

    /// <summary>
    /// Gets the observers of this queue that have a stored durable subscription, read from storage.
    /// </summary>
    /// <returns>The identifiers of the observers, or none when storage is not available to the queue.</returns>
    /// <remarks>
    /// An observer belongs to the queue its identifier hashes to, which is the queue
    /// <see cref="AppendedEventsQueueRouter"/> routes its subscription to. This is the read a queue seeds from once
    /// durable subscriptions are acted on (decision 0034, point 2); in the shadow stage it is only compared.
    /// </remarks>
    internal async Task<IReadOnlyList<ObserverId>> GetStoredDurableSubscribers()
    {
        if (_storage is null || _queueCount < 1)
        {
            return [];
        }

        var router = new AppendedEventsQueueRouter(_queueCount);
        var stored = await _storage
            .GetEventStore(_eventSequenceKey.EventStore)
            .GetNamespace(_eventSequenceKey.Namespace)
            .Observers
            .GetDurableSubscriptions(_eventSequenceKey.EventSequenceId);
        return [.. stored.Keys.Where(observer => router.GetQueueIndexFor(new ObserverKey(observer, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceKey.EventSequenceId)) == _queueId)];
    }

    /// <summary>
    /// Compares the live subscriptions of this queue with the stored durable subscriptions, and reports the difference.
    /// </summary>
    /// <returns>The disagreements found.</returns>
    /// <remarks>
    /// Only reports. A stored subscription without a live one is not woken and a live one without a stored one is not
    /// recorded; the shadow stage exists to show how often they disagree before anything depends on the record.
    /// </remarks>
    internal async Task<AppendedEventsQueueDurableSubscriptionDisagreements> CompareDurableSubscriptions()
    {
        // Read the live set after the store, so a subscription made while the read ran counts as live rather than as missing.
        var stored = await GetStoredDurableSubscribers();
        IReadOnlyList<ObserverId> live;
        lock (_subscriptionsLock)
        {
            live = [.. _subscriptions.Select(subscription => subscription.ObserverKey.ObserverId)];
        }

        var disagreements = AppendedEventsQueueDurableSubscriptionDisagreements.Compare(stored, live);
        if (!disagreements.Agree)
        {
            _metrics?.DurableSubscriptionsDisagreed(AppendedEventsQueueDurableSubscriptionDisagreements.StoredNotLiveKind, disagreements.StoredNotLive.Count);
            _metrics?.DurableSubscriptionsDisagreed(AppendedEventsQueueDurableSubscriptionDisagreements.LiveNotStoredKind, disagreements.LiveNotStored.Count);
            _logger.DurableSubscriptionsDisagree(_eventSequenceKey, disagreements.StoredNotLive.Count, disagreements.LiveNotStored.Count);
        }

        return disagreements;
    }

    void ConfigureDurableSubscriptionCheck(Observers observers)
    {
        if (_storage is null || observers.DurableSubscriptions != DurableSubscriptionMode.Shadow)
        {
            return;
        }

        var interval = observers.DurableSubscriptionCheckInterval > TimeSpan.Zero
            ? observers.DurableSubscriptionCheckInterval
            : TimeSpan.FromMinutes(15);

        // The first comparison waits a full interval, plus a spread so the queues of every namespace do not read together:
        // sooner than that the observers of a restarted kernel are still subscribing, and every comparison would disagree.
        var spread = TimeSpan.FromTicks((long)(interval.Ticks * 0.25 * Random.Shared.NextDouble()));
        this.RegisterGrainTimer(CheckDurableSubscriptions, new GrainTimerCreationOptions { DueTime = interval + spread, Period = interval });
    }

    async Task CheckDurableSubscriptions()
    {
        try
        {
            await CompareDurableSubscriptions();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.DurableSubscriptionCheckFailed(_eventSequenceKey, exception);
        }
    }
}
