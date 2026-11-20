// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.EventSequences;

/// <content>
/// The part of the queue that keeps the subscriptions of dormant observers and wakes them (decision 0034).
/// </content>
/// <remarks>
/// A dormant observer has no activation, so nothing in it can ask to be woken. The queue is the one thing that already
/// sees every event for it, so it keeps the observer's subscription entry across the release, rings the observer (which
/// activates it from its durable subscription) when a matching event arrives, and wakes it on a jittered interval to run
/// one watchdog pass. A dormant observer holds an entry in a dictionary here and nothing else: no timer, no task, no lane.
/// </remarks>
public partial class AppendedEventsQueue
{
    /// <summary>
    /// The fraction of the safety net interval after which the first wake of a dormant observer is due, at the earliest.
    /// </summary>
    internal const double SafetyNetEarliest = 0.6;

    /// <summary>
    /// The fraction of the safety net interval after which the first wake of a dormant observer is due, at the latest.
    /// </summary>
    /// <remarks>
    /// Below 1 so that the wake, the granularity of the tick that drives it and the time the wake itself takes all fit in the
    /// interval: a lost wake-up delays delivery by at most the safety net interval.
    /// </remarks>
    internal const double SafetyNetLatest = 0.9;

    static readonly TimeSpan _maxSafetyNetTick = TimeSpan.FromSeconds(60);
    static readonly TimeSpan _minSafetyNetTick = TimeSpan.FromSeconds(1);

    readonly Dictionary<ObserverKey, DateTimeOffset> _dormant = [];
    readonly IClusterReleases? _clusterReleases;
    readonly IWakeAdmission? _wakeAdmission;
    bool _dormantSeedPending;

    /// <summary>
    /// Gets whether dormant observers are kept and woken by this queue right now.
    /// </summary>
    /// <remarks>
    /// Evaluated at every decision rather than once, because a rolling upgrade can add an older silo to the cluster.
    /// </remarks>
    internal bool DormancyEnabled =>
        _storage is not null &&
        _observers.DurableSubscriptions == DurableSubscriptionMode.Release &&
        _observers.DormantAfter > 0 &&
        DormantObserverFloor.IsReached(_clusterReleases);

    /// <summary>
    /// Gets the number of subscriptions the queue holds as dormant.
    /// </summary>
    internal int DormantCount
    {
        get
        {
            lock (_subscriptionsLock)
            {
                return _dormant.Count;
            }
        }
    }

    /// <summary>
    /// Gets the interval of the safety net.
    /// </summary>
    internal TimeSpan SafetyNetInterval => TimeSpan.FromSeconds(_observers.DormantSafetyNet < 1 ? 900 : _observers.DormantSafetyNet);

    /// <summary>
    /// Gets how often the safety net looks for dormant observers that are due.
    /// </summary>
    internal TimeSpan SafetyNetTick => TimeSpan.FromTicks(Math.Clamp(SafetyNetInterval.Ticks / 10, _minSafetyNetTick.Ticks, _maxSafetyNetTick.Ticks));

    /// <inheritdoc/>
    public Task<bool> TryMarkDormant(ObserverKey observerKey)
    {
        if (_isDisposed || _isDraining || !DormancyEnabled)
        {
            return Task.FromResult(false);
        }

        lock (_subscriptionsLock)
        {
            if (_isResetting || !_subscriptions.Exists(subscription => subscription.ObserverKey == observerKey))
            {
                return Task.FromResult(false);
            }

            // Events still queued for the observer, or being delivered to it, would be stranded by a release.
            if (_lanes.TryGetValue(observerKey, out var lane) && !lane.IsClosed && (lane.PendingBatches > 0 || lane.PendingEvents > 0))
            {
                return Task.FromResult(false);
            }

            _dormant[observerKey] = NextSafetyNetDue();
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> IsDormant(ObserverKey observerKey)
    {
        lock (_subscriptionsLock)
        {
            return Task.FromResult(!_isResetting && _dormant.ContainsKey(observerKey));
        }
    }

    /// <summary>
    /// Gets the events of a batch that an observer's subscription takes, in sequence order, as live delivery would deliver them.
    /// </summary>
    /// <param name="events">The events, in any order.</param>
    /// <param name="subscription">The subscription that selects them.</param>
    /// <returns>The events the subscription matches, in sequence order.</returns>
    internal static IReadOnlyList<AppendedEvent> SelectForSubscription(IEnumerable<AppendedEvent> events, AppendedEventsQueueObserverSubscription subscription) =>
        GetFilteredEvents(SortBySequenceNumber([.. events]), subscription);

    /// <summary>
    /// Splits events in sequence order into runs of one partition each, as live delivery hands them to an observer.
    /// </summary>
    /// <param name="events">The events, in sequence order.</param>
    /// <returns>The runs, each with the partition and its events.</returns>
    internal static List<(EventSourceId Partition, AppendedEvent[] Events)> SplitIntoRuns(IReadOnlyList<AppendedEvent> events) =>
        SplitIntoPartitionRuns(events);

    /// <summary>
    /// Reads the durable subscriptions of the queue's observers and keeps a subscription entry for each of them.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Runs when the queue activates, before it takes a single event: an append cannot be enqueued to a grain that is still
    /// activating, so an event is never routed to a queue that has not seen the dormant observers. The entry holds what the
    /// queue needs to route (the observer, its event types and filters); the event types come from the observer's definition,
    /// never from the record, so a changed definition cannot leave a stale type list behind. An observer that is already
    /// subscribed keeps its entry.
    /// </remarks>
    internal async Task SeedDormantSubscriptions()
    {
        _dormantSeedPending = false;
        if (!DormancyEnabled || _queueCount < 1)
        {
            return;
        }

        try
        {
            var eventStore = _storage!.GetEventStore(_eventSequenceKey.EventStore);
            var stored = await eventStore
                .GetNamespace(_eventSequenceKey.Namespace)
                .Observers
                .GetDurableSubscriptions(_eventSequenceKey.EventSequenceId);

            var router = new AppendedEventsQueueRouter(_queueCount);
            var seeded = 0;
            foreach (var (observerId, record) in stored)
            {
                var key = new ObserverKey(observerId, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceKey.EventSequenceId);
                if (!record.Kind.CanBeReleasedWhenDormant() || router.GetQueueIndexFor(key) != _queueId || !await eventStore.Observers.Has(observerId))
                {
                    continue;
                }

                var definition = await eventStore.Observers.Get(observerId);
                var subscription = new AppendedEventsQueueObserverSubscription(
                    key,
                    [.. definition.EventTypes.Select(eventType => eventType.Id)],
                    record.Filters?.ToObserverFilters(),
                    !definition.EventTypes.Any());
                lock (_subscriptionsLock)
                {
                    if (_isResetting || _subscriptions.Exists(existing => existing.ObserverKey == key))
                    {
                        continue;
                    }

                    _subscriptions.Add(subscription);
                    _dormant[key] = NextSafetyNetDue();
                    seeded++;
                }
            }

            _logger.SeededDormantSubscriptions(_eventSequenceKey, seeded);
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            // The queue still serves the observers that subscribe themselves; the safety net seeds again.
            _dormantSeedPending = true;
            _logger.SeedingDormantSubscriptionsFailed(_eventSequenceKey, exception);
        }
    }

    /// <summary>
    /// Wakes every dormant observer whose safety net interval has elapsed.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The wake runs one watchdog pass in the observer (which checks that its client is still there and that nothing is owed)
    /// and the observer releases itself again. The interval of each observer is jittered, so the wakes of the observers of a
    /// namespace spread over the interval instead of lining up, and each takes a place in the silo's wake admission.
    /// </remarks>
    internal async Task RunDormantSafetyNet()
    {
        if (_isDisposed || _isDraining)
        {
            return;
        }

        if (_dormantSeedPending)
        {
            await SeedDormantSubscriptions();
        }

        if (!DormancyEnabled)
        {
            return;
        }

        var due = TakeDueDormantObservers();
        if (due.Count == 0)
        {
            return;
        }

        await Task.WhenAll(due.Select(WakeForSafetyNet));
    }

    void ConfigureDormantSafetyNet()
    {
        if (_storage is null || _observers.DurableSubscriptions != DurableSubscriptionMode.Release || _observers.DormantAfter <= 0)
        {
            return;
        }

        var tick = SafetyNetTick;
        this.RegisterGrainTimer(RunDormantSafetyNet, new GrainTimerCreationOptions { DueTime = tick, Period = tick });
    }

    DateTimeOffset NextSafetyNetDue() =>
        CatchupTimeProvider.GetUtcNow() + (SafetyNetInterval * (SafetyNetEarliest + ((SafetyNetLatest - SafetyNetEarliest) * Random.Shared.NextDouble())));

    List<ObserverKey> TakeDueDormantObservers()
    {
        var now = CatchupTimeProvider.GetUtcNow();
        var due = new List<ObserverKey>();
        lock (_subscriptionsLock)
        {
            foreach (var (key, dueAt) in _dormant.ToArray())
            {
                if (!_subscriptions.Exists(subscription => subscription.ObserverKey == key))
                {
                    _dormant.Remove(key);
                    continue;
                }

                if (dueAt > now)
                {
                    continue;
                }

                // Rescheduled before the wake runs: a wake that fails is tried again one interval later, not on the next tick.
                _dormant[key] = NextSafetyNetDue();
                due.Add(key);
            }
        }

        return due;
    }

    async Task WakeForSafetyNet(ObserverKey observerKey)
    {
        try
        {
            await WithWakeAdmission(() => _grainFactory.GetGrain<IObserver>(observerKey).RunWatchdogOnce());
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            _logger.SafetyNetWakeFailed(observerKey, exception);
        }
    }

    async Task WithWakeAdmission(Func<Task> wake)
    {
        if (_wakeAdmission is null)
        {
            await wake();
            return;
        }

        await _wakeAdmission.WaitAsync(_catchupCancellationToken);
        try
        {
            await wake();
        }
        finally
        {
            _wakeAdmission.Release();
        }
    }

    bool TakeDormantMark(ObserverKey observerKey)
    {
        lock (_subscriptionsLock)
        {
            return _dormant.Remove(observerKey);
        }
    }

    /// <summary>
    /// Activates a dormant observer, under the wake admission of the silo, before events are delivered to it.
    /// </summary>
    /// <param name="lane">The <see cref="AppendedEventsQueueObserverLane"/> that is about to deliver.</param>
    /// <param name="events">The events the lane is about to deliver.</param>
    /// <returns>True when the lane may deliver; false when waking failed and the observer was handed to catch-up.</returns>
    /// <remarks>
    /// The events are the doorbell, not the data: the woken observer reads from its own cursor and delivers what it finds, and
    /// the events delivered afterwards are filtered by that cursor, so an event is delivered exactly once.
    /// </remarks>
    async Task<bool> WakeDormantObserver(AppendedEventsQueueObserverLane lane, IReadOnlyList<AppendedEvent> events)
    {
        if (!TakeDormantMark(lane.ObserverKey))
        {
            return true;
        }

        try
        {
            await WithWakeAdmission(lane.Observer.Ensure);
            return true;
        }
        catch (OperationCanceledException) when (_catchupCancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            SpillFailedSubscriptionToCatchup(CurrentSubscriptionOf(lane), [.. events]);
            LaneFailed(exception);
            return false;
        }
    }
}
