// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.ObserverRouting;

namespace Cratis.Chronicle.EventSequences;

/// <content>
/// The part of the queue that rings the doorbell of observers it holds no live subscription for.
/// </content>
/// <remarks>
/// <para>
/// The queue knows which observers take which events from the silo-local routing table, and which of them have an instance in
/// its namespace from their stored cursors. An instance with no live subscription here has no activation, or has one that lost
/// the subscription (the queue was collected, or spilled it). When an event it takes arrives, or the safety net finds an event
/// waiting after its cursor, the queue rings it: the observer runs one watchdog pass, which resumes it from its cursor and
/// takes its live subscription again. The event is the doorbell, not the data, so nothing is lost while it is not subscribed.
/// </para>
/// <para>
/// Nothing is seeded and nothing is activated ahead of an event: the queue holds a cursor per instance and a backoff per
/// observer it rang, nothing else. An observer that stays unsubscribed after a ring, such as one whose client is gone, is rung
/// again only after a backoff that doubles each time, so it does not cost an activation per event.
/// </para>
/// </remarks>
public partial class AppendedEventsQueue
{
    /// <summary>
    /// The fraction of the safety net interval after which the next pass is due, at the earliest.
    /// </summary>
    internal const double SafetyNetEarliest = 0.6;

    /// <summary>
    /// The fraction of the safety net interval after which the next pass is due, at the latest.
    /// </summary>
    internal const double SafetyNetLatest = 0.9;

    /// <summary>
    /// The shortest time between two rings of the same observer that did not subscribe in between.
    /// </summary>
    internal static readonly TimeSpan MinimumRingBackoff = TimeSpan.FromMinutes(1);

    static readonly TimeSpan _maxSafetyNetTick = TimeSpan.FromSeconds(60);
    static readonly TimeSpan _minSafetyNetTick = TimeSpan.FromSeconds(1);

    readonly Dictionary<ObserverId, RingBackoff> _rings = [];
    readonly HashSet<ObserverId> _releasedWhileDormant = [];
    readonly IWakeAdmission? _wakeAdmission;
    readonly IDormantObserverGauge? _dormantObserverGauge;
    IDisposable? _dormantGaugeRegistration;
    IReadOnlyDictionary<ObserverId, EventSequenceNumber> _instances = new Dictionary<ObserverId, EventSequenceNumber>();
    DateTimeOffset _nextSafetyNetPass;
    int _ringsInFlight;

    /// <summary>
    /// Gets the number of instances served by this queue that hold no live subscription here.
    /// </summary>
    internal int DormantCount
    {
        get
        {
            lock (_subscriptionsLock)
            {
                return _instances.Keys.Count(id => IsServedHere(id) && !_subscriptions.Exists(subscription => subscription.ObserverKey.ObserverId == id));
            }
        }
    }

    /// <summary>
    /// Gets the interval of the safety net.
    /// </summary>
    internal TimeSpan SafetyNetInterval => TimeSpan.FromSeconds(_observers.DormantSafetyNet < 1 ? 900 : _observers.DormantSafetyNet);

    /// <summary>
    /// Gets the longest time between two rings of the same observer that did not subscribe in between.
    /// </summary>
    internal TimeSpan MaximumRingBackoff
    {
        get
        {
            var configured = TimeSpan.FromSeconds(_observers.DormantSafetyNetFullWake < 1 ? 3600 : _observers.DormantSafetyNetFullWake);
            return configured < MinimumRingBackoff ? MinimumRingBackoff : configured;
        }
    }

    /// <summary>
    /// Gets how long one keep of the activation lasts; renewed by every safety net tick and every admitted batch while work is owed.
    /// </summary>
    internal TimeSpan KeepAlivePeriod => TimeSpan.FromTicks(Math.Max(SafetyNetTick.Ticks * 3, TimeSpan.FromMinutes(2).Ticks));

    /// <summary>
    /// Gets how often the safety net looks whether a pass is due.
    /// </summary>
    internal TimeSpan SafetyNetTick => TimeSpan.FromTicks(Math.Clamp(SafetyNetInterval.Ticks / 10, _minSafetyNetTick.Ticks, _maxSafetyNetTick.Ticks));

    /// <inheritdoc/>
    /// <remarks>
    /// The queue keeps nothing for a dormant observer: its live subscription is dropped, and the routing table and the
    /// observer's stored cursor are what the queue rings it by. Refused while events are queued for the observer or being
    /// delivered to it, so a release never strands events in a lane.
    /// </remarks>
    public Task<bool> TryMarkDormant(ObserverKey observerKey)
    {
        if (_isDisposed || _isDraining)
        {
            return Task.FromResult(false);
        }

        lock (_subscriptionsLock)
        {
            if (_isResetting || !_subscriptions.Exists(subscription => subscription.ObserverKey == observerKey))
            {
                return Task.FromResult(false);
            }

            if (_lanes.TryGetValue(observerKey, out var lane) && !lane.IsClosed && (lane.PendingBatches > 0 || lane.PendingEvents > 0))
            {
                return Task.FromResult(false);
            }

            _subscriptions.RemoveAll(subscription => subscription.ObserverKey == observerKey);
            CloseLane(observerKey);
            _pageObservers.TryRemove(observerKey, out _);
            _releasedWhileDormant.Add(observerKey.ObserverId);
        }

        Observe(() => _metrics?.ObserverReleasedWhileDormant());
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Only an observer this queue released while it was subscribed is dormant. One that unsubscribed, or that this
    /// activation of the queue never released, is not: it is asked directly, which activates it if it has to.
    /// </remarks>
    public Task<bool> IsDormant(ObserverKey observerKey)
    {
        lock (_subscriptionsLock)
        {
            return Task.FromResult(
                !_isResetting &&
                _releasedWhileDormant.Contains(observerKey.ObserverId) &&
                !_subscriptions.Exists(subscription => subscription.ObserverKey == observerKey));
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
    /// Reads which observers have an instance in the namespace of the queue, from their stored cursors.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// One read of the cursors of the event sequence. An observer that runs for the first time in the namespace subscribes,
    /// which makes it an instance here without waiting for the next read.
    /// </remarks>
    internal async Task ReadInstances()
    {
        if (_nextSafetyNetPass == default)
        {
            _nextSafetyNetPass = NextSafetyNetPass(CatchupTimeProvider.GetUtcNow());
        }

        if (_storage is null || _queueCount < 1)
        {
            return;
        }

        try
        {
            var cursors = await _storage
                .GetEventStore(_eventSequenceKey.EventStore)
                .GetNamespace(_eventSequenceKey.Namespace)
                .Observers
                .GetNextEventSequenceNumbers(_eventSequenceKey.EventSequenceId);
            lock (_subscriptionsLock)
            {
                _instances = cursors;
            }
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            // The instances read earlier stay; the next pass of the safety net reads them again.
            _logger.SeedingDormantSubscriptionsFailed(_eventSequenceKey, exception);
        }
    }

    /// <summary>
    /// Rings the observers that take events of a routed batch and hold no live subscription here.
    /// </summary>
    /// <param name="table">The <see cref="ObserverRoutingTable"/> of the event store.</param>
    /// <param name="subscriptions">The subscriptions the batch was routed to.</param>
    /// <param name="batchEventTypes">The distinct event types of the batch.</param>
    internal void RingUnsubscribedObservers(ObserverRoutingTable table, AppendedEventsQueueObserverSubscription[] subscriptions, HashSet<EventTypeId> batchEventTypes)
    {
        if (RefusesNewWork || IsStopping)
        {
            return;
        }

        var subscribed = subscriptions.Select(_ => _.ObserverKey.ObserverId).ToHashSet();
        var toRing = batchEventTypes
            .Where(_ => _ != GlobalEventTypes.Redaction)
            .SelectMany(eventTypeId => table.Match(_eventSequenceKey.EventSequenceId, eventTypeId))
            .Select(route => route.ObserverId)
            .Distinct()
            .Where(id => !subscribed.Contains(id) && IsInstance(id) && IsServedHere(id))
            .ToArray();

        foreach (var observerId in toRing)
        {
            Ring(observerId, WakeCauses.Event);
        }
    }

    /// <summary>
    /// Rings the instances served by this queue that hold no live subscription here and have an event waiting after their cursor.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The cursors are read once per pass and the tails once per observed event type, before any ring, so the cost of a pass
    /// does not grow with the number of instances that turn out to be up to date. A check that cannot tell rings, so it errs
    /// toward delivery. Passes are spread over the interval so the queues of every namespace do not read together.
    /// </remarks>
    internal async Task RunDormantSafetyNet()
    {
        KeepWhileOwed();
        if (RefusesNewWork || _routing is null || _storage is null)
        {
            return;
        }

        var now = CatchupTimeProvider.GetUtcNow();
        if (now < _nextSafetyNetPass)
        {
            return;
        }

        _nextSafetyNetPass = NextSafetyNetPass(now);
        await ReadInstances();
        var table = await GetShadowRoutingTable();
        if (table is null)
        {
            return;
        }

        ObserverBehindCheckCandidate[] candidates;
        lock (_subscriptionsLock)
        {
            candidates = [.. _instances.Keys
                .Where(id => IsServedHere(id) && !_subscriptions.Exists(subscription => subscription.ObserverKey.ObserverId == id) && !IsBackingOff(id, now))
                .Select(id => table.TryGetRoute(id, out var route) ? route : null)
                .OfType<ObserverRoute>()
                .Where(route => route.EventSequenceId == _eventSequenceKey.EventSequenceId)
                .Select(route => new ObserverBehindCheckCandidate(KeyOf(route.ObserverId), route.EventTypes, route.AllEventTypes))];
        }

        if (candidates.Length == 0)
        {
            return;
        }

        var (behind, unverified, upToDate) = await FindObserversThatAreBehind(candidates);
        Observe(() => _metrics?.DormantSafetyNetChecked(SafetyNetOutcomes.Behind, behind.Count));
        Observe(() => _metrics?.DormantSafetyNetChecked(SafetyNetOutcomes.Unverified, unverified.Count));
        Observe(() => _metrics?.DormantSafetyNetChecked(SafetyNetOutcomes.UpToDate, upToDate));
        foreach (var observerKey in behind.Concat(unverified))
        {
            Ring(observerKey.ObserverId, WakeCauses.SafetyNet);
        }
    }

    /// <summary>
    /// Compares the cursor of each observer with the tail of the event types it observes.
    /// </summary>
    /// <param name="candidates">The observers to compare.</param>
    /// <returns>The observers with an event waiting, the ones that could not be compared, and how many are up to date.</returns>
    /// <remarks>
    /// The comparison is the shared <see cref="IObserverBehindCheck"/>. A check that cannot read errs toward delivery and
    /// reports every observer as unverified. An event appended after the tail was read rings the observer as it arrives.
    /// </remarks>
    async Task<(List<ObserverKey> Behind, List<ObserverKey> Unverified, int UpToDate)> FindObserversThatAreBehind(ObserverBehindCheckCandidate[] candidates)
    {
        try
        {
            var result = await new ObserverBehindCheck(_storage!, _grainFactory).Check(_eventSequenceKey, candidates);
            return ([.. result.Behind], [.. result.Unverified], result.UpToDate);
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            _logger.SafetyNetCheckFailed(_eventSequenceKey, exception);
            return ([], [.. candidates.Select(candidate => candidate.ObserverKey)], 0);
        }
    }

    void ConfigureDormantSafetyNet()
    {
        if (_storage is null || _routing is null)
        {
            return;
        }

        var tick = SafetyNetTick;
        this.RegisterGrainTimer(RunDormantSafetyNet, new GrainTimerCreationOptions { DueTime = tick, Period = tick });
    }

    DateTimeOffset NextSafetyNetPass(DateTimeOffset now) =>
        now + (SafetyNetInterval * (SafetyNetEarliest + ((SafetyNetLatest - SafetyNetEarliest) * Random.Shared.NextDouble())));

    ObserverKey KeyOf(ObserverId observerId) =>
        new(observerId, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceKey.EventSequenceId);

    bool IsInstance(ObserverId observerId)
    {
        lock (_subscriptionsLock)
        {
            return _instances.ContainsKey(observerId);
        }
    }

    bool IsServedHere(ObserverId observerId) =>
        _queueCount < 1 || new AppendedEventsQueueRouter(_queueCount).GetQueueIndexFor(observerId) == _queueId;

    bool IsBackingOff(ObserverId observerId, DateTimeOffset now) =>
        _rings.TryGetValue(observerId, out var backoff) && backoff.NotBefore > now;

    /// <summary>
    /// Makes the observer known to the queue as an instance with a live subscription, which ends any backoff of its rings.
    /// Must be called holding the subscriptions lock.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer that subscribed.</param>
    void NoteSubscribed(ObserverKey observerKey)
    {
        _rings.Remove(observerKey.ObserverId);
        _releasedWhileDormant.Remove(observerKey.ObserverId);
        if (!_instances.ContainsKey(observerKey.ObserverId))
        {
            _instances = new Dictionary<ObserverId, EventSequenceNumber>(_instances) { [observerKey.ObserverId] = EventSequenceNumber.Unavailable };
        }
    }

    void Ring(ObserverId observerId, string cause)
    {
        var now = CatchupTimeProvider.GetUtcNow();
        lock (_subscriptionsLock)
        {
            if (_isDisposed || _isResetting || IsBackingOff(observerId, now))
            {
                return;
            }

            var backoff = _rings.TryGetValue(observerId, out var previous)
                ? TimeSpan.FromTicks(Math.Min(previous.Backoff.Ticks * 2, MaximumRingBackoff.Ticks))
                : MinimumRingBackoff;
            _rings[observerId] = new(now + backoff, backoff);
        }

        Interlocked.Increment(ref _ringsInFlight);
        KeepWhileOwed();
        var observerKey = KeyOf(observerId);
        _ = DetachedExecution.Start(() => _taskFactory.Run(async () =>
        {
            try
            {
                await WithWakeAdmission(() => _grainFactory.GetGrain<IObserver>(observerKey).RunWatchdogOnce(), cause);
            }
            catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
            {
                _logger.SafetyNetWakeFailed(observerKey, exception);
            }
            finally
            {
                Interlocked.Decrement(ref _ringsInFlight);
            }
        }));
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

    /// <summary>
    /// Keeps the activation while the queue holds anything that is not in storage: live subscriptions, queued or in-flight
    /// deliveries, catch-up triggers or rings. Orleans does not see the background work, so it is told.
    /// </summary>
    void KeepWhileOwed()
    {
        bool owed;
        lock (_subscriptionsLock)
        {
            owed = !_isDisposed && (_subscriptions.Count > 0 || _lanes.Count > 0 || _catchupTriggers.Count > 0);
        }

        // A queue constructed directly, outside an activation, has nothing to keep.
        if (GrainContext is not null && (owed || Volatile.Read(ref _pendingItems) > 0 || Volatile.Read(ref _ringsInFlight) > 0))
        {
            DelayDeactivation(KeepAlivePeriod);
        }
    }

    /// <summary>
    /// The backoff of the rings of one observer.
    /// </summary>
    /// <param name="NotBefore">When the observer may be rung next.</param>
    /// <param name="Backoff">The backoff the next ring is scheduled with.</param>
    [StructLayout(LayoutKind.Auto)]
    readonly record struct RingBackoff(DateTimeOffset NotBefore, TimeSpan Backoff);
}
