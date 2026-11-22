// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
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

    readonly Dictionary<ObserverKey, DormantEntry> _dormant = [];
    readonly IClusterReleases? _clusterReleases;
    readonly IWakeAdmission? _wakeAdmission;
    readonly IDormantObserverGauge? _dormantObserverGauge;
    IDisposable? _dormantGaugeRegistration;
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
    /// Gets how long a dormant observer goes without being woken, whether or not an event is waiting for it.
    /// </summary>
    /// <remarks>
    /// Never shorter than <see cref="SafetyNetInterval"/>: at that value every check is a wake, as before the check existed.
    /// </remarks>
    internal TimeSpan FullWakeInterval
    {
        get
        {
            var configured = TimeSpan.FromSeconds(_observers.DormantSafetyNetFullWake < 1 ? 3600 : _observers.DormantSafetyNetFullWake);
            return configured < SafetyNetInterval ? SafetyNetInterval : configured;
        }
    }

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

            _dormant[observerKey] = NewDormantEntry();
        }

        Observe(() => _metrics?.ObserverReleasedWhileDormant());
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
                    _dormant[key] = NewDormantEntry();
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
    /// Wakes the dormant observers whose safety net interval has elapsed and that have something to find out.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// <para>
    /// A check compares the persisted cursor of each due observer with the tail of the event types it observes and wakes it
    /// only when an event is waiting, or when the cursor or the tail cannot be read: a check that cannot tell wakes, so it
    /// errs toward delivery. An observer that is up to date is not activated and is looked at again one interval later.
    /// The cursors of the event sequence are read in one read and the tails in one lookup per observed event type, before
    /// any wake, so the cost of a pass does not grow with the number of observers that turn out to be up to date.
    /// </para>
    /// <para>
    /// A wake runs one watchdog pass in the observer (which checks that its client is still there and that nothing is owed)
    /// and the observer releases itself again. What only a running observer can find out cannot be seen from the cursor, so
    /// an observer that has gone <see cref="FullWakeInterval"/> without a wake is woken whatever the comparison says. The
    /// interval of each observer is jittered, so the checks of the observers of a namespace spread over the interval instead
    /// of lining up, and each wake takes a place in the silo's wake admission.
    /// </para>
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

        var (full, checks) = TakeDueDormantObservers();
        if (full.Count == 0 && checks.Count == 0)
        {
            return;
        }

        var toWake = new List<ObserverKey>(full);
        Observe(() => _metrics?.DormantSafetyNetChecked(SafetyNetOutcomes.FullWake, full.Count));

        if (checks.Count > 0)
        {
            var (behind, unverified, upToDate) = await FindObserversThatAreBehind(checks);
            toWake.AddRange(behind);
            toWake.AddRange(unverified);
            Observe(() => _metrics?.DormantSafetyNetChecked(SafetyNetOutcomes.Behind, behind.Count));
            Observe(() => _metrics?.DormantSafetyNetChecked(SafetyNetOutcomes.Unverified, unverified.Count));
            Observe(() => _metrics?.DormantSafetyNetChecked(SafetyNetOutcomes.UpToDate, upToDate));
        }

        if (toWake.Count == 0 || _isDisposed || _isDraining)
        {
            return;
        }

        await Task.WhenAll(toWake.Select(WakeForSafetyNet));
    }

    static async Task<Dictionary<EventTypeId, EventSequenceNumber>> ReadTailPerEventType(IEventSequence eventSequence, EventTypeId[] typeIds)
    {
        var tails = await Task.WhenAll(typeIds.Select(typeId => eventSequence.GetTailSequenceNumberForEventTypes([new EventType(typeId, EventTypeGeneration.First)])));
        return typeIds.Zip(tails).ToDictionary(pair => pair.First, pair => pair.Second);
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

    /// <summary>
    /// Creates the entry of an observer that has just become dormant: when it is first looked at and when it is first woken regardless.
    /// </summary>
    /// <returns>The <see cref="DormantEntry"/>.</returns>
    /// <remarks>
    /// One draw sets both, so the unconditional wake is never due before the first check, and the two coincide when the
    /// intervals are the same.
    /// </remarks>
    DormantEntry NewDormantEntry()
    {
        var fraction = SafetyNetEarliest + ((SafetyNetLatest - SafetyNetEarliest) * Random.Shared.NextDouble());
        var now = CatchupTimeProvider.GetUtcNow();
        return new(now + (SafetyNetInterval * fraction), now + (FullWakeInterval * fraction));
    }

    DateTimeOffset NextSafetyNetDue() =>
        CatchupTimeProvider.GetUtcNow() + (SafetyNetInterval * (SafetyNetEarliest + ((SafetyNetLatest - SafetyNetEarliest) * Random.Shared.NextDouble())));

    (List<ObserverKey> Full, List<ObserverKey> Checks) TakeDueDormantObservers()
    {
        var now = CatchupTimeProvider.GetUtcNow();
        var full = new List<ObserverKey>();
        var checks = new List<ObserverKey>();
        lock (_subscriptionsLock)
        {
            foreach (var (key, entry) in _dormant.ToArray())
            {
                if (!_subscriptions.Exists(subscription => subscription.ObserverKey == key))
                {
                    _dormant.Remove(key);
                    continue;
                }

                if (entry.CheckDue > now)
                {
                    continue;
                }

                // Rescheduled before the wake runs: a wake that fails is tried again one interval later, not on the next tick.
                if (entry.FullWakeDue <= now)
                {
                    _dormant[key] = NewDormantEntry();
                    full.Add(key);
                    continue;
                }

                _dormant[key] = entry with { CheckDue = NextSafetyNetDue() };
                checks.Add(key);
            }
        }

        return (full, checks);
    }

    /// <summary>
    /// Compares the cursor of each observer with the tail of the event types it observes.
    /// </summary>
    /// <param name="observers">The dormant observers that are due.</param>
    /// <returns>The observers with an event waiting, the ones that could not be compared, and how many are up to date.</returns>
    /// <remarks>
    /// The cursors are read before the tails. An event appended in between is then part of the tail but not of the cursor, so
    /// the observer is found behind and woken once more than it needed to be; the other order could miss it until the next
    /// interval. An event appended after the tail was read rings the observer's doorbell, as it always does.
    /// </remarks>
    async Task<(List<ObserverKey> Behind, List<ObserverKey> Unverified, int UpToDate)> FindObserversThatAreBehind(List<ObserverKey> observers)
    {
        AppendedEventsQueueObserverSubscription[] subscriptions;
        lock (_subscriptionsLock)
        {
            subscriptions = [.. observers
                .Select(key => _subscriptions.Find(subscription => subscription.ObserverKey == key))
                .OfType<AppendedEventsQueueObserverSubscription>()];
        }

        var behind = new List<ObserverKey>();
        var unverified = new List<ObserverKey>();
        var upToDate = 0;

        IReadOnlyDictionary<ObserverId, EventSequenceNumber> cursors;
        DormantTails tails;
        try
        {
            cursors = await _storage!
                .GetEventStore(_eventSequenceKey.EventStore)
                .GetNamespace(_eventSequenceKey.Namespace)
                .Observers
                .GetNextEventSequenceNumbers(_eventSequenceKey.EventSequenceId);
            tails = await ReadTails(subscriptions.Where(subscription => cursors.TryGetValue(subscription.ObserverKey.ObserverId, out var cursor) && cursor.IsActualValue));
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            _logger.SafetyNetCheckFailed(_eventSequenceKey, exception);
            return (behind, [.. subscriptions.Select(subscription => subscription.ObserverKey)], 0);
        }

        foreach (var subscription in subscriptions)
        {
            if (!cursors.TryGetValue(subscription.ObserverKey.ObserverId, out var cursor) || !cursor.IsActualValue)
            {
                unverified.Add(subscription.ObserverKey);
            }
            else if (tails.HasEventsFrom(subscription, cursor))
            {
                behind.Add(subscription.ObserverKey);
            }
            else
            {
                upToDate++;
            }
        }

        return (behind, unverified, upToDate);
    }

    async Task<DormantTails> ReadTails(IEnumerable<AppendedEventsQueueObserverSubscription> subscriptions)
    {
        var comparable = subscriptions.ToArray();
        var eventSequence = _grainFactory.GetGrain<IEventSequence>(_eventSequenceKey);

        // A redaction stands in for the event type it redacted, so it is part of what every observer of types looks at.
        var typeIds = comparable
            .Where(subscription => !subscription.AllEventTypes)
            .SelectMany(subscription => subscription.EventTypeIdSet)
            .Append(GlobalEventTypes.Redaction)
            .Distinct()
            .ToArray();

        var perType = typeIds.Length == 0 || comparable.All(subscription => subscription.AllEventTypes)
            ? []
            : await ReadTailPerEventType(eventSequence, typeIds);
        var tail = comparable.Any(subscription => subscription.AllEventTypes)
            ? await eventSequence.GetTailSequenceNumber()
            : EventSequenceNumber.Unavailable;
        return new(perType, tail);
    }

    async Task WakeForSafetyNet(ObserverKey observerKey)
    {
        try
        {
            await WithWakeAdmission(() => _grainFactory.GetGrain<IObserver>(observerKey).RunWatchdogOnce(), WakeCauses.SafetyNet);
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            _logger.SafetyNetWakeFailed(observerKey, exception);
        }
    }

    async Task WithWakeAdmission(Func<Task> wake, string cause)
    {
        Observe(() => _metrics?.DormantObserverWoken(cause));
        if (_wakeAdmission is null)
        {
            await wake();
            return;
        }

        if (_wakeAdmission.InProgress >= _wakeAdmission.Limit)
        {
            Observe(() => _metrics?.DormantWakeDelayed());
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
        // A queue that is draining, or a silo that is stopping, must not activate a grain: the activation is refused or outlives
        // the stop. The observer stays dormant, its events are durable, and the safety net of the queue that owns it next wakes
        // it and it resumes from its cursor.
        if (_isDraining || _stopBudget?.IsStopping == true)
        {
            return false;
        }

        if (!TakeDormantMark(lane.ObserverKey))
        {
            return true;
        }

        try
        {
            await WithWakeAdmission(lane.Observer.Ensure, WakeCauses.Event);
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

    /// <summary>
    /// What the queue holds for a dormant observer between its release and its wake.
    /// </summary>
    /// <param name="CheckDue">When the safety net compares the observer's cursor with the tail next.</param>
    /// <param name="FullWakeDue">When the safety net wakes the observer whatever the comparison says.</param>
    [StructLayout(LayoutKind.Auto)]
    readonly record struct DormantEntry(DateTimeOffset CheckDue, DateTimeOffset FullWakeDue);

    /// <summary>
    /// The tails of an event sequence that a pass of the safety net compares the cursors with.
    /// </summary>
    /// <param name="PerEventType">The sequence number of the last event of each event type that was looked up.</param>
    /// <param name="Tail">The sequence number of the last event of the sequence, or unavailable when it was not looked up.</param>
    sealed record DormantTails(IReadOnlyDictionary<EventTypeId, EventSequenceNumber> PerEventType, EventSequenceNumber Tail)
    {
        /// <summary>
        /// Gets whether an event the subscription observes was appended at or after a cursor.
        /// </summary>
        /// <param name="subscription">The <see cref="AppendedEventsQueueObserverSubscription"/> of the observer.</param>
        /// <param name="cursor">The next sequence number the observer expects.</param>
        /// <returns>True when it was; otherwise false.</returns>
        public bool HasEventsFrom(AppendedEventsQueueObserverSubscription subscription, EventSequenceNumber cursor)
        {
            if (subscription.AllEventTypes)
            {
                return Tail.IsActualValue && Tail >= cursor;
            }

            return subscription.EventTypeIdSet.Append(GlobalEventTypes.Redaction).Any(typeId =>
                PerEventType.TryGetValue(typeId, out var tail) && tail.IsActualValue && tail >= cursor);
        }
    }
}
