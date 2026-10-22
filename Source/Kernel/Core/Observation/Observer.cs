// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.DevelopmentTools;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Placement;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.StateMachines;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserver"/>.
/// </summary>
/// <param name="observerDefinition"><see cref="IPersistentState{T}"/> for the observer definition.</param>
/// <param name="failures"><see cref="IPersistentState{T}"/> for failed partitions.</param>
/// <param name="configurationProvider">The <see cref="IConfigurationForObserverProvider"/> for getting the <see cref="Observers"/> configuration.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="eventCompliance"><see cref="IEventCompliance"/> for decrypting PII fields in event content.</param>
/// <param name="subscriberSelector"><see cref="IObserverSubscriberSelector"/> for selecting which connected client instance to deliver to.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="meter"><see cref="Meter{T}"/> for the observer.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
/// <param name="optionsMonitor">The webhook retry configuration.</param>
/// <param name="eventGenerationRelease">The pinned generation release boundary.</param>
/// <param name="timeProvider">The delivery fence clock.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ObserverState)]
[KeepAlive]
[ObserverPlacement]
public partial class Observer(
    [PersistentState(nameof(ObserverDefinition), WellKnownGrainStorageProviders.ObserverDefinitions)]
    IPersistentState<ObserverDefinition> observerDefinition,
    [PersistentState(nameof(FailedPartition), WellKnownGrainStorageProviders.FailedPartitions)]
    IPersistentState<FailedPartitions> failures,
    IConfigurationForObserverProvider configurationProvider,
    IStorage storage,
    IEventCompliance eventCompliance,
    IObserverSubscriberSelector subscriberSelector,
    ILogger<Observer> logger,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<Observer> meter,
    [FromKeyedServices(WellKnown.MeterName)] IActivitySource<Observer> activitySource,
    ILoggerFactory loggerFactory,
    IOptionsMonitor<ChronicleOptions> optionsMonitor,
    IEventGenerationRelease? eventGenerationRelease = null,
    TimeProvider? timeProvider = null) : StateMachine<ObserverState>, IObserver, IObserverDevelopmentReset, IRemindable, IDisposable
{
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly HashSet<JobId> _concludedCatchUpJobs = [];

    /// <summary>
    /// The catch-up job acquisition currently in flight, if any.
    /// </summary>
    /// <remarks>
    /// A job being started is not listed by the jobs manager until its start completes, so a catch-up arriving in the
    /// meantime - routing after an interleaved <see cref="CaughtUp(JobId, EventSequenceNumber)"/>, or the appended-events queue triggering one -
    /// would find no owner and start a second job over the same events. It adopts the outcome of this acquisition instead.
    /// </remarks>
    Task<JobId>? _pendingCatchUpAcquisition;

    ObserverId _observerId = ObserverId.Unspecified;
    ObserverKey _observerKey = ObserverKey.NotSet;
    ObserverSubscription _subscription = ObserverSubscription.Unsubscribed;
    IJobsManager _jobsManager = null!;
    bool _stateWritingSuspended;
    bool _resumingQuarantine;

    /// <summary>
    /// Installation precedes job recovery. Until setup returns successfully, managers must retry rather
    /// than caching the installed subscription as established. Catch-up completion is a separate concern.
    /// </summary>
    bool _subscriptionSetupFailed;
    bool _isQuarantined;
    bool _recoverSubscriptionAfterQuarantine;
    bool _retryRecoveryAfterQuarantine;
    int _owedRecoveryAttempts;
    int _recoveryGeneration;
    int _recoveriesInProgress;

    /// <summary>
    /// Set once the observer has been removed, so nothing this activation does afterwards writes it back.
    /// </summary>
    /// <remarks>
    /// Deactivation normally flushes progress and transitions to <see cref="Disconnected"/>, both of which persist
    /// observer state. For a removed observer that would recreate the very documents the removal deleted, moments
    /// after deleting them, and leave the store looking exactly as it did before.
    /// </remarks>
    bool _removed;
    IEventSequence _eventSequence = null!;
    IAppendedEventsQueues _appendedEventsQueues = null!;
    IMeterScope<Observer>? _metrics;
    bool _isPreparingCatchup;
    int _catchUpHandoversInFlight;

    /// <summary>
    /// Completes when the last catch-up handover in flight has finished routing; null while no handover is in flight.
    /// </summary>
    /// <remarks>
    /// A handover routes the observer from an interleaving <see cref="CaughtUp(JobId, EventSequenceNumber)"/> call. A transition requested while that
    /// routing is in progress is only scheduled, and routing's own choice of next state replaces it. <see cref="Replay"/>
    /// waits on this before transitioning, so its transition runs in its own turn and is never deferred or replaced.
    /// </remarks>
    TaskCompletionSource? _catchUpHandoversSettled;

    /// <summary>
    /// Counts every time catch-up ownership started moving - a handover entering or an acquisition starting.
    /// </summary>
    /// <remarks>
    /// A handover can both start and finish while the watchdog awaits its job lookup, leaving nothing in flight to
    /// see afterwards and a listing that predates the successor it started. A changed epoch is what reveals it.
    /// </remarks>
    int _catchUpOwnershipEpoch;
    int _catchupRecoveryAttempts;

    /// <summary>
    /// Counts consecutive catch-up laps that concluded without moving the observer forward.
    /// </summary>
    int _zeroWorkCatchUps;

    /// <summary>
    /// The moment from which a catch-up that produced no work may be tried again, or the default value when none is backing off.
    /// </summary>
    DateTimeOffset _catchUpBackoffUntil;
    Dictionary<EventType, EventTypeSchema> _eventTypeSchemas = [];
    int _statePersistenceBatchInterval = 1;
    TimeSpan _writeCoalescingInterval = TimeSpan.Zero;
    Dictionary<Key, Dictionary<EventTypeId, EventCount>> _pendingHandledCounts = [];
    IGrainTimer? _coalescedWriteTimer;
    bool _coalescedWriteBroughtForward;
    bool _failureStateWritePending;
    Dictionary<FailedPartitionId, TimeSpan> _pendingRetryReminders = [];
    int _debouncedProgressWrites;

    /// <inheritdoc/>
    protected override Type InitialState => typeof(Routing);

    /// <summary>
    /// Gets whether the activation is quarantined, independently of reloadable stored-state snapshots.
    /// </summary>
    bool IsQuarantined => _isQuarantined;

    ObserverDefinition Definition => observerDefinition.State;

    /// <summary>
    /// Gets the <see cref="ObserverReaderScope"/> every job request and key index lookup of this observer carries.
    /// </summary>
    /// <remarks>
    /// For now this is exactly what the observer's definition and subscription mode say it reads. Decision 0010
    /// section 8 later makes an observer that is not subscribed to all events and has no event types read nothing
    /// rather than everything; until then it is mapped to <see cref="ObserverReaderScope.Wildcard"/>, which is what
    /// its readers have always done with an empty type list.
    /// </remarks>
    ObserverReaderScope ReaderScope => ObserverReaderScope.For(State.SubscribesToAllEvents, Definition.EventTypes);

    FailedPartitions Failures => failures.State;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await ReconcilePartitionDeferrals();
        RememberHandledEventsReportsForTheResponseTimeout();
        ScheduleAlertReport();
    }

    /// <inheritdoc/>
    public override async Task OnActivation(CancellationToken cancellationToken)
    {
        _observerKey = ObserverKey.Parse(this.GetPrimaryKeyString());
        _observerId = _observerKey.ObserverId;
        State = State with { Identifier = _observerId };
        SeedSuspectedSkipOwnership();

        _jobsManager = GrainFactory.GetJobsManager(_observerKey.EventStore, _observerKey.Namespace);

        await failures.ReadStateAsync();
        await RestoreDeliveryFences();

        _eventSequence = GrainFactory.GetGrain<IEventSequence>(
            new EventSequenceKey(_observerKey.EventSequenceId, _observerKey.EventStore, _observerKey.Namespace));

        var eventSequenceKey = new EventSequenceKey(_observerKey.EventSequenceId, _observerKey.EventStore, _observerKey.Namespace);
        _appendedEventsQueues = GrainFactory.GetGrain<IAppendedEventsQueues>(eventSequenceKey);
        _metrics = meter.BeginObserverScope(_observerId, _observerKey);

        var config = await configurationProvider.GetFor(_observerKey);
        _statePersistenceBatchInterval = config.StatePersistenceBatchInterval < 1 ? 1 : config.StatePersistenceBatchInterval;
        _writeCoalescingInterval = TimeSpan.FromSeconds(Math.Max(config.WriteCoalescingInterval, 0));
        RegisterWatchdog(config.WatchdogInterval);
        await InitializeAlertState();
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        try
        {
            if (_removed || !_observerExists)
            {
                await base.OnDeactivateAsync(reason, cancellationToken);
                return;
            }

            await FlushDebouncedProgressState();
            if (reason.ReasonCode != DeactivationReasonCode.ShuttingDown)
            {
                await TransitionTo<Disconnected>();
                await base.OnDeactivateAsync(reason, cancellationToken);
            }
        }
        finally
        {
            Dispose();
        }
    }

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ResetForDevelopment()
    {
        if (_removed)
        {
            return;
        }

        await Unsubscribe();

        // DeactivateOnIdle only schedules deactivation. Seal first so late callbacks and OnDeactivateAsync cannot
        // re-persist the old cursor after the resetter wipes storage. This uses the same seal as observer removal,
        // without deleting any persisted definition here.
        _removed = true;
        DeactivateOnIdle();
    }

#pragma warning disable CA1721 // Property names should not match get methods
    /// <inheritdoc/>
    public Task<ObserverDefinition> GetDefinition() => Task.FromResult(observerDefinition.State);

    /// <inheritdoc/>
    public Task<ObserverState> GetState()
    {
        return Task.FromResult(State);
    }
#pragma warning restore CA1721 // Property namTes should not match get methods

    /// <inheritdoc/>
    public Task<ObserverSubscription> GetSubscription() => Task.FromResult(_subscription);

    /// <inheritdoc/>
    public Task<bool> IsSubscribed() => Task.FromResult(_subscription.IsSubscribed);

    /// <inheritdoc/>
    public Task<bool> IsSubscriptionEstablished() => Task.FromResult(_subscription.IsSubscribed && !_subscriptionSetupFailed);

    /// <inheritdoc/>
    public Task<bool> IsPreparingCatchup() => Task.FromResult(_isPreparingCatchup);

    /// <inheritdoc/>
    public Task<bool> IsCatchUpBackedOff() => Task.FromResult(IsBackingOffCatchUp);

    /// <inheritdoc/>
    public Task<bool> HasFailedPartitions() => Task.FromResult(Failures.HasFailedPartitions);

    /// <inheritdoc/>
    public Task<bool> IsObserverQuarantined() => Task.FromResult(IsQuarantined);

    /// <inheritdoc/>
    public Task<IEnumerable<Key>> GetFailedPartitionKeys() => Task.FromResult(Failures.Partitions.Select(p => p.Partition));

    /// <inheritdoc/>
    public async Task ClearObserverQuarantine()
    {
        ThrowIfSealed();
        if (IsQuarantined)
        {
            await ReviveFromQuarantine();
        }
        else if (_retryRecoveryAfterQuarantine && _subscription.IsSubscribed)
        {
            // The quarantine ended but recovering its subscription never reached a settled state - recovery or the
            // persistence of a transition failed, dropping the transition that would have carried it on. Clearing
            // again retries that recovery rather than doing nothing.
            await RetryRecoveryAfterQuarantine();
        }
    }

    /// <inheritdoc/>
    public async Task Remove()
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.RemovingObserver();

        ThrowIfSealed();

        // Normal subscription calls cannot interleave this namespace-local operation. The store-wide guard
        // is only a preflight check; earlier namespaces may already be removed if this recheck fails.
        if (_subscription.IsSubscribed || State.RunningState == ObserverRunningState.Active)
        {
            throw new ObserverRemovalNotAllowed(_observerKey);
        }

        if (_observerExists)
        {
            await CommitRetired();
            await RequireAlertReconciliation();
        }

        // Jobs may call interleaved recovery methods while stopping. Do not hold their mutation lock here.
        var jobs = await _jobsManager.GetAllJobs();
        foreach (var job in jobs.Where(job => job.Request is IObserverJobRequest request && request.ObserverKey.ObserverId == _observerId))
        {
            await _jobsManager.Delete(job.Id);
        }

        await _alertMutationLock.WaitAsync();
        await _stateWriteLock.WaitAsync();
        try
        {
            await RemoveFailedPartitionReminders();
            var namespaceStorage = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace);
            await namespaceStorage.FailedPartitions.RemoveAllFor(_observerId);
            await namespaceStorage.ObserverHandledCounts.RemoveAllFor(_observerId);
            foreach (var inFlight in await namespaceStorage.InFlightEvents.GetFor(_observerId))
            {
                await namespaceStorage.InFlightEvents.Remove(_observerId, inFlight.Partition, inFlight.EventSequenceNumber);
            }

            // The deletion can commit and then throw. Seal this activation before attempting it so neither
            // callbacks nor deactivation can recreate the record. Reactivation consults authoritative storage.
            _removed = true;
            _stateWritingSuspended = true;
            try
            {
                await namespaceStorage.Observers.Delete(_observerId);
                _observerExists = false;
                await UnregisterReminderNamed(AlertReminderName);
            }
            finally
            {
                DeactivateOnIdle();
            }
        }
        finally
        {
            _stateWriteLock.Release();
            _alertMutationLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventType>> GetEventTypes()
    {
        // An observer subscribed to all events has no fixed event type list to return - the whole
        // point is that it also covers types that did not exist when it subscribed. Resolve the full,
        // current set from storage each time rather than a snapshot captured at subscribe time.
        if (State.SubscribesToAllEvents)
        {
            var schemas = await storage.GetEventStore(_observerKey.EventStore).EventTypes.GetLatestForAllEventTypes();
            return schemas.Select(_ => _.Type);
        }

        return Definition.EventTypes;
    }

    /// <inheritdoc/>
    public Task Subscribe<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = null,
        bool isReplayable = true,
        ObserverFilters? filters = null,
        bool reactivateRetired = true,
        bool automatic = false)
        where TObserverSubscriber : IObserverSubscriber
        => SubscribeWithGenerationDelivery<TObserverSubscriber>(type, eventTypes, siloAddress, EventGenerationDelivery.Compatibility, subscriberArgs, isReplayable, filters, reactivateRetired, automatic);

    /// <inheritdoc/>
    public Task SubscribeWithGenerationDelivery<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        EventGenerationDelivery generationDelivery,
        object? subscriberArgs = null,
        bool isReplayable = true,
        ObserverFilters? filters = null,
        bool reactivateRetired = true,
        bool automatic = false)
        where TObserverSubscriber : IObserverSubscriber =>
        SubscribeToEventTypes<TObserverSubscriber>(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, reactivateRetired: reactivateRetired, automatic: automatic, generationDelivery: generationDelivery);

    /// <inheritdoc/>
    public async Task SubscribeToAllEvents<TObserverSubscriber>(
        ObserverType type,
        SiloAddress siloAddress,
        object? subscriberArgs = null,
        bool isReplayable = true,
        bool reactivateRetired = true)
        where TObserverSubscriber : IObserverSubscriber
    {
        ThrowIfSealed();
        SupersedeRecoveryAfterQuarantine();
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        logger.Subscribing();
        logger.SubscribingToAllEvents();

        await ReloadObserverState();
        await observerDefinition.ReadStateAsync();
        await failures.ReadStateAsync();
        if (!reactivateRetired && State.AlertDisposition == AlertDisposition.Retired) return;
        await BeginAlertLifecycle();
        await LeaveQuarantineForSubscription();

        _subscriptionSetupFailed = true;
        try
        {
            await SetUpAllEventsSubscription<TObserverSubscriber>(type, siloAddress, subscriberArgs, isReplayable);
            await RecoverAfterSubscribing(leavesQuarantine: true);
            _subscriptionSetupFailed = await GetCurrentState() is CatchingUpInFlight;
        }
        catch
        {
            _subscriptionSetupFailed = true;
            throw;
        }
    }

    /// <inheritdoc/>
    public override IImmutableList<IState<ObserverState>> CreateStates() => new IState<ObserverState>[]
    {
        new Disconnected(),

        new Routing(
            _observerKey,
            observerDefinition,
            _eventSequence,
            storage,
            loggerFactory.CreateLogger<Routing>()),

        new Replay(
            _observerKey,
            observerDefinition,
            _jobsManager,
            storage,
            loggerFactory.CreateLogger<Replay>()),

        new QuarantinedObserver(
            _observerKey,
            loggerFactory.CreateLogger<QuarantinedObserver>()),

        new CatchingUpInFlight(
            _observerKey,
            observerDefinition,
            failures,
            _jobsManager,
            loggerFactory.CreateLogger<CatchingUpInFlight>()),

        new Observing(
            _appendedEventsQueues,
            _observerKey.EventStore,
            _observerKey.Namespace,
            _observerKey.EventSequenceId,
            observerDefinition,
            _eventSequence,
            loggerFactory.CreateLogger<Observing>())
    }.ToImmutableList();

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        SupersedeRecoveryAfterQuarantine();
        _subscription = ObserverSubscription.Unsubscribed;
        ForgetSupersededRegistrations();
        await PauseJobs();
        await TransitionTo<Disconnected>();
    }

    /// <inheritdoc/>
    public async Task UnsubscribeIfMatchesClient(ConnectionId connectionId)
    {
        // Single-threaded grain — the check and the subscription change form an atomic action.
        // If a new client has already replaced the subscription, the old client's
        // disconnect cleanup must not tear down the new client's subscription.
        if (_subscription.IsSubscribed && _subscription.Targets.Count > 0)
        {
            var remaining = _subscription.Targets
                .Where(target => target.ConnectedClient!.ConnectionId != connectionId)
                .ToArray();
            if (remaining.Length == _subscription.Targets.Count)
            {
                DropSupersededRegistration(connectionId);
                return;
            }

            if (remaining.Length > 0)
            {
                _subscription = _subscription with
                {
                    SiloAddress = remaining[0].SiloAddress,
                    Arguments = remaining[0].ConnectedClient,
                    Targets = remaining
                };
                return;
            }

            await PromoteSupersededRegistrationOrUnsubscribe();
            return;
        }

        if (_subscription.IsSubscribed &&
            _subscription.Arguments is ConnectedClient connectedClient &&
            connectedClient.ConnectionId != connectionId)
        {
            DropSupersededRegistration(connectionId);
            return;
        }

        await Unsubscribe();
    }

    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName == AlertReminderName)
        {
            await ReconcileAlertsIfNeeded();
            return;
        }

        if (!Failures.Partitions.Any(_ => IsRetryReminderFor(_, reminderName) && _.Deferral is not null))
        {
            await UnregisterReminderNamed(reminderName);
        }

        if (CannotRecoverFromReminder())
        {
            return;
        }

        // Accept raw names registered by main as well as bounded episode-specific retry names.
        foreach (var partition in Failures.Partitions.Where(partition => IsRetryReminderFor(partition, reminderName)))
        {
            await StartRecoverJobForFailedPartition(partition);
        }
    }

    /// <summary>
    /// Gets a retry reminder name in a namespace disjoint from the alert reminder, regardless of the partition key.
    /// </summary>
    /// <param name="failureId">The failure episode to retry.</param>
    /// <returns>The bounded retry reminder name.</returns>
    internal static string PartitionReminderName(FailedPartitionId failureId) => $"cp:{failureId.Value:N}";

    /// <summary>
    /// Set subscription explicitly, without subscribing. This method is internal and visible to the test suite and only meant to be used with testing.
    /// </summary>
    /// <param name="subscription">Subscription to set.</param>
    internal void SetSubscription(ObserverSubscription subscription)
    {
        _subscription = subscription;
    }

    /// <summary>
    /// Gets whether a recovery owed after a quarantine ended has not yet settled, so the next clearance retries it.
    /// This is internal and visible to the test suite only.
    /// </summary>
    /// <returns>True if the recovery is still owed, false if not.</returns>
    internal bool OwesRecoveryAfterQuarantine() => _retryRecoveryAfterQuarantine;

    /// <summary>
    /// Records, in the observer's metrics, that the observer was quarantined.
    /// </summary>
    /// <remarks>
    /// Entering <see cref="QuarantinedObserver"/> on activation resumes a quarantine that was already counted when it
    /// began, so that entry is not counted again.
    /// </remarks>
    internal void RecordObserverQuarantined()
    {
        if (_resumingQuarantine)
        {
            _resumingQuarantine = false;
            return;
        }

        _metrics?.ObserverQuarantined();
    }

    /// <summary>
    /// Removes all reminders for currently failed partitions.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task RemoveFailedPartitionReminders()
    {
        foreach (var partition in Failures.Partitions)
        {
            await RemoveReminder(partition);
        }
    }

    /// <summary>
    /// Stops all retry jobs for the current observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task StopAllRetryFailedPartitionJobs()
    {
        var jobs = await _jobsManager.GetUnfinishedJobs();
        var stopTasks = jobs
            .Where(_ => _.Request is RetryFailedPartitionRequest request && request.ObserverKey == _observerKey)
            .Select(_ => _jobsManager.Stop(_.Id));
        await Task.WhenAll(stopTasks);
    }

    /// <summary>
    /// Resolves the state to enter on activation, retaining quarantine and keeping retired observers
    /// disconnected. Their replay metadata is retained for a fresh subscription, not a reason to start work.
    /// </summary>
    /// <returns>The type of the state to enter.</returns>
    protected override Type ResolveActivationState()
    {
        if (State.RunningState != ObserverRunningState.Quarantined)
        {
            return !_observerExists || IsRetired ? typeof(Disconnected) : base.ResolveActivationState();
        }

        _resumingQuarantine = true;
        return typeof(QuarantinedObserver);
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeEnteringState(IState<ObserverState> state)
    {
        ResetRecoveryFlagsForEntering(state);
        _isQuarantined = state is QuarantinedObserver;
        await _alertMutationLock.WaitAsync();
        try
        {
            if (state is BaseObserverState observerState)
            {
                TrackQuarantineAlertFor(observerState);
                State = State with { RunningState = observerState.RunningState };
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }

        BeginTransitionHook();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterEnteringState(IState<ObserverState> state)
    {
        KeepWhatWasRecordedDuringTransitionHook();
        if (SettlesOwedRecovery(state))
        {
            // Settled: the observer is driven forward again, or routing legitimately disconnected an empty
            // explicitly-typed subscription. The initial Disconnected entry still owes its recovery.
            _retryRecoveryAfterQuarantine = false;
            _owedRecoveryAttempts = 0;
            return;
        }

        if (state is Disconnected && _recoverSubscriptionAfterQuarantine)
        {
            _recoverSubscriptionAfterQuarantine = false;
            if (_subscription.IsSubscribed)
            {
                await RecoverAfterQuarantine();
            }
        }
    }

    /// <inheritdoc/>
    protected override async Task WriteStateAsync()
    {
        if (CannotWriteState()) return;
        await _stateWriteLock.WaitAsync();
        try
        {
            if (_removed || CannotWriteState()) return;

            // A quarantine owned by this activation outranks whatever running state a reloaded snapshot carried,
            // so any write while quarantined persists the quarantine rather than a stale active snapshot.
            if (IsQuarantined)
            {
                State = State with { RunningState = ObserverRunningState.Quarantined };
            }

            // A state-machine OnEnter can return an earlier record after awaiting a job callback. Preserve the
            // source-owned metadata advanced by that callback, and serialize writes from AlwaysInterleave methods.
            State = State with
            {
                AlertLifecycleId = _alertLifecycleId,
                AlertRevision = _alertRevision,
                AlertDisposition = _alertDisposition,
                QuarantineEpisodeId = _quarantineEpisodeId,
                DeliveryFences = DeliveryFenceSnapshot()
            };
            var revision = _alertRevision;
            await base.WriteStateAsync();
            if (revision == _alertRevision)
            {
                _alertStateNeedsPersistence = false;
            }

            _debouncedProgressWrites = 0;
        }
        finally
        {
            _stateWriteLock.Release();
        }
    }

    static bool IsRetryReminderFor(FailedPartition partition, string reminderName) =>
        !partition.IsQuarantined &&
        (PartitionReminderName(partition.Id) == reminderName || partition.Partition.ToString() == reminderName);

    static void ThrowIfPinsMultipleGenerations(IEnumerable<EventType> eventTypes)
    {
        var duplicate = eventTypes.GroupBy(_ => _.Id).FirstOrDefault(_ => _.Select(type => type.Generation).Distinct().Count() > 1);
        if (duplicate is not null)
        {
            throw new ObserverPinsMultipleGenerationsOfEventType(duplicate.Key);
        }
    }

    static bool FiltersAreEqual(ObserverFilters? left, ObserverFilters? right)
    {
        if (left is null || right is null)
        {
            return ReferenceEquals(left, right);
        }

        // ObserverFilters is a record, but its Tags collection makes the generated equality a
        // reference comparison - two identical registrations from different client instances
        // would never be considered equal. Compare structurally instead.
        return left.Tags.ToHashSet().SetEquals(right.Tags) &&
               Equals(left.EventSourceType, right.EventSourceType) &&
               Equals(left.EventStreamType, right.EventStreamType);
    }

    bool SettlesOwedRecovery(IState<ObserverState> state) =>
        state is Observing or States.Replay || IsEmptySubscriptionDisconnect(state);

    bool IsEmptySubscriptionDisconnect(IState<ObserverState> state) =>
        state is Disconnected &&
        !_recoverSubscriptionAfterQuarantine &&
        _subscription.IsSubscribed &&
        !_subscription.EventTypes.Any() &&
        !State.SubscribesToAllEvents;

    bool CannotWriteState() => _stateWritingSuspended || !_observerExists;

    bool CannotRecoverFromReminder() => IsRetired || _removed || IsQuarantined || !_subscription.IsSubscribed;

    void ResetRecoveryFlagsForEntering(IState<ObserverState> state)
    {
        // Any transition supersedes a recovery scheduled for when a requested leave enters Disconnected. Entering
        // Disconnected is how that leave completes, so only that keeps it.
        if (state is not Disconnected)
        {
            _recoverSubscriptionAfterQuarantine = false;
        }

        // A recovery owed after quarantine stays retryable until it settles. Entering a transient state does not
        // settle it: the transition carrying it onward is dropped if persisting that entry fails. A new quarantine
        // supersedes it, since clearing that one owes a recovery of its own.
        if (state is QuarantinedObserver)
        {
            _retryRecoveryAfterQuarantine = false;
            _owedRecoveryAttempts = 0;
        }
    }

    void TrackQuarantineAlertFor(BaseObserverState observerState)
    {
        var wasQuarantined = State.RunningState == ObserverRunningState.Quarantined;
        var isQuarantined = observerState.RunningState == ObserverRunningState.Quarantined;
        if (isQuarantined && !wasQuarantined)
        {
            _quarantineEpisodeId = Guid.NewGuid();
            ChangeAlertState();
        }
        else if (!isQuarantined && wasQuarantined)
        {
            if (_quarantineEpisodeId is { } episode && !_alertEndings.ContainsKey(new(episode)))
            {
                RememberQuarantineEnding(Concepts.Alerts.AlertClearedReason.Cleared);
            }

            _quarantineEpisodeId = null;
            ChangeAlertState();
        }
    }

    /// <summary>
    /// Reloads stored progress without replacing this activation's identity or quarantine.
    /// </summary>
    /// <returns>True if storage no longer has a record for this observer, false if it does.</returns>
    async Task<bool> ReloadObserverState()
    {
        await ReadStateAsync();
        var storageWasReset = State.Identifier == ObserverId.Unspecified;
        State = State with { Identifier = _observerId };
        if (IsQuarantined)
        {
            State = State with { RunningState = ObserverRunningState.Quarantined };
        }

        // A reload during a transition hook is newer than the snapshot the hook was handed; keep it from being reverted.
        RecordedHandlingState();
        return storageWasReset;
    }

    async Task SubscribeToEventTypes<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        ObserverFilters? filters,
        bool additive = false,
        bool recovering = false,
        bool reactivateRetired = true,
        bool automatic = false,
        EventGenerationDelivery generationDelivery = EventGenerationDelivery.Compatibility)
        where TObserverSubscriber : IObserverSubscriber
    {
        eventTypes = eventTypes.ToArray();
        await ValidateGenerationDelivery(eventTypes, generationDelivery);

        if (!automatic)
        {
            _recoverSubscriptionAfterQuarantine = false;
        }

        // Automatic kernel subscription cannot revive a retired observer or a sealed activation.
        // Ordinary Subscribe keeps its explicit-registration semantics, including throwing when sealed.
        if (IsAdditiveSubscriptionBlocked(additive)) return;
        ThrowIfSealed();
        if (recovering && !await NeedsSubscriptionRecovery(eventTypes)) return;

        // Only a subscription that may end quarantine supersedes recovery owed by an earlier clearance.
        var leavesQuarantine = !automatic && !recovering;
        if (leavesQuarantine)
        {
            SupersedeRecoveryAfterQuarantine();
        }

        if (additive)
        {
            // Merge inside the serialized observer turn, retaining newer concurrent registrations.
            eventTypes = MergeSubscribedEventTypes(eventTypes);
        }

        try
        {
            var eventTypeSchemas = await storage.GetEventStore(_observerKey.EventStore).EventTypes.GetFor(eventTypes);
            _eventTypeSchemas = eventTypeSchemas.ToDictionary(s => s.Type);

            using var scope = logger.BeginObserverScope(_observerId, _observerKey);

            // Ordinary subscription and automatic reconciliation re-read storage, including after a shared
            // test silo's database reset, but must not let a stale snapshot overwrite an interleaved quarantine.
            // Recovery retains live progress rather than reloading a stale persisted position.
            if (!recovering)
            {
                var wasQuarantined = IsQuarantined;
                var storageWasReset = await ReloadObserverState();

                await observerDefinition.ReadStateAsync();
                await failures.ReadStateAsync();
                if (storageWasReset && wasQuarantined && IsQuarantined)
                {
                    // The quarantine belonged to a wiped world. Discard it only after all state has been reloaded,
                    // so routing cannot act on stale definitions or failures from that world.
                    DiscardQuarantineFromWipedWorld();
                    await TransitionTo<Routing>();
                }
            }

            if (IsRetiredAndNotReactivating(reactivateRetired)) return;
            await BeginAlertLifecycle();
            if (recovering)
            {
                // A failed entry write can strand the machine in CatchingUpInFlight, which cannot enter
                // itself. Leave it without resetting progress or granting authority to release quarantine.
                _subscriptionSetupFailed = true;
                await TransitionTo<Disconnected>();
            }
            else if (!automatic)
            {
                await LeaveQuarantineForSubscription();
            }

            _subscriptionSetupFailed = true;
            await SetUpSubscription<TObserverSubscriber>(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, generationDelivery);
            await RecoverAfterSubscribing(leavesQuarantine);

            // CatchingUpInFlight always schedules an onward transition on entry. Remaining there after
            // recovery returns means a failed entry write stranded it, and an ordinary retry could not re-enter.
            // Healthy asynchronous catch-up and deferred routing are separate from that setup failure.
            _subscriptionSetupFailed = await GetCurrentState() is CatchingUpInFlight;
        }
        catch
        {
            _subscriptionSetupFailed = true;
            throw;
        }
    }

    bool IsRetiredAndNotReactivating(bool reactivateRetired) => !reactivateRetired && State.AlertDisposition == AlertDisposition.Retired;

    bool IsAdditiveSubscriptionBlocked(bool additive) => additive && (_removed || IsRetired);

    void DiscardQuarantineFromWipedWorld()
    {
        _recoverSubscriptionAfterQuarantine = false;
        _isPreparingCatchup = false;
        NotePreparationEnded();
        _catchupRecoveryAttempts = 0;
        _subscription = ObserverSubscription.Unsubscribed;
        ForgetSupersededRegistrations();
    }

    async Task SetUpSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        ObserverFilters? filters,
        EventGenerationDelivery generationDelivery,
        bool reconcile = false)
        where TObserverSubscriber : IObserverSubscriber
    {
        if (reconcile && CannotReconcileSubscription()) return;
        var owner = GetOwner<TObserverSubscriber>();
        logger.Subscribing();
        logger.SubscribingWithEventTypes(eventTypes.Count(), string.Join(", ", eventTypes.Select(et => et.Id)));

        observerDefinition.State = observerDefinition.State with
        {
            Type = type,
            Owner = owner,
            EventTypes = eventTypes,
            IsReplayable = isReplayable,
            GenerationDelivery = generationDelivery
        };
        await observerDefinition.WriteStateAsync();

        if (reconcile && CannotReconcileSubscription()) return;
        ApplySubscription<TObserverSubscriber>(eventTypes, siloAddress, subscriberArgs, isReplayable, filters, generationDelivery);

        State = State with { SubscribesToAllEvents = false };
        await WriteStateAsync();
    }

    async Task SetUpAllEventsSubscription<TObserverSubscriber>(
        ObserverType type,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        bool reconcile = false)
        where TObserverSubscriber : IObserverSubscriber
    {
        if (reconcile && CannotReconcileSubscription()) return;
        observerDefinition.State = observerDefinition.State with
        {
            Type = type,
            Owner = GetOwner<TObserverSubscriber>(),
            EventTypes = [],
            IsReplayable = isReplayable
        };
        await observerDefinition.WriteStateAsync();

        if (reconcile && CannotReconcileSubscription()) return;
        _subscription = new(
            _observerId,
            _observerKey,
            [],
            typeof(TObserverSubscriber),
            siloAddress,
            subscriberArgs,
            isReplayable);
        ForgetSupersededRegistrations();

        State = State with { SubscribesToAllEvents = true };
        await WriteStateAsync();
    }

    async Task ValidateGenerationDelivery(IEnumerable<EventType> eventTypes, EventGenerationDelivery generationDelivery)
    {
        if (generationDelivery != EventGenerationDelivery.Pinned) return;
        ThrowIfPinsMultipleGenerations(eventTypes);
        var eventTypesStorage = storage.GetEventStore(_observerKey.EventStore).EventTypes;
        foreach (var eventType in eventTypes)
        {
            if (!await eventTypesStorage.HasFor(eventType.Id, eventType.Generation))
            {
                throw new EventTypeGenerationNotRegisteredForObserver(eventType);
            }
        }
    }

    void ApplySubscription<TObserverSubscriber>(
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        ObserverFilters? filters,
        EventGenerationDelivery generationDelivery)
        where TObserverSubscriber : IObserverSubscriber
    {
        if (subscriberArgs is not ConnectedClient connectedClient)
        {
            ForgetSupersededRegistrations();
            _subscription = new(
                _observerId,
                _observerKey,
                eventTypes,
                typeof(TObserverSubscriber),
                siloAddress,
                subscriberArgs,
                isReplayable,
                filters)
            {
                GenerationDelivery = generationDelivery
            };
            return;
        }

        var target = new ObserverSubscriberTarget(siloAddress, connectedClient);
        if (CanFanOutInto<TObserverSubscriber>(eventTypes, filters, generationDelivery))
        {
            DropSupersededRegistration(connectedClient.ConnectionId);

            // Another instance of the same client is already subscribed with an identical
            // definition - add this instance as a fan-out target instead of replacing the
            // subscription. The stable ordering keeps partition selection deterministic.
            var targets = _subscription.Targets
                .Where(existing => existing.ConnectedClient!.ConnectionId != connectedClient.ConnectionId)
                .Append(target)
                .OrderBy(existing => existing.ConnectedClient!.ConnectionId.Value)
                .ToArray();
            _subscription = _subscription with
            {
                SiloAddress = targets[0].SiloAddress,
                Arguments = targets[0].ConnectedClient,
                Targets = targets
            };
            return;
        }

        // The registrations whose targets this replaces are kept, so the observer can hand over to one of them
        // when the client that now owns the subscription leaves.
        SupersedeTargetsOtherThan(connectedClient.ConnectionId);
        var observerType = observerDefinition.State.Type;
        _resubscribeTarget = existing => SubscribeToEventTypes<TObserverSubscriber>(
            observerType,
            eventTypes,
            existing.SiloAddress,
            existing.ConnectedClient,
            isReplayable,
            filters,
            reactivateRetired: false,
            automatic: true,
            generationDelivery: generationDelivery);

        _subscription = new(
            _observerId,
            _observerKey,
            eventTypes,
            typeof(TObserverSubscriber),
            siloAddress,
            subscriberArgs,
            isReplayable,
            filters)
        {
            Targets = [target],
            GenerationDelivery = generationDelivery
        };
    }

    bool CanFanOutInto<TObserverSubscriber>(IEnumerable<EventType> eventTypes, ObserverFilters? filters, EventGenerationDelivery generationDelivery)
        where TObserverSubscriber : IObserverSubscriber =>
        _subscription.IsSubscribed &&
        _subscription.Targets.Count > 0 &&
        _subscription.SubscriberType == typeof(TObserverSubscriber) &&
        _subscription.GenerationDelivery == generationDelivery &&
        _subscription.EventTypes.ToHashSet().SetEquals(eventTypes) &&
        FiltersAreEqual(_subscription.Filters, filters);

    void RemoveSubscriberTarget(ObserverSubscriberTarget target)
    {
        var remaining = _subscription.Targets
            .Where(existing => existing.ConnectedClient?.ConnectionId != target.ConnectedClient?.ConnectionId)
            .ToArray();
        _subscription = remaining.Length > 0
            ? _subscription with
            {
                SiloAddress = remaining[0].SiloAddress,
                Arguments = remaining[0].ConnectedClient,
                Targets = remaining
            }
            : _subscription with { Targets = [] };
    }

    ObserverOwner GetOwner<TObserverSubscriber>()
        where TObserverSubscriber : IObserverSubscriber => typeof(TObserverSubscriber) switch
        {
            Type t when t.IsAssignableTo(typeof(IAmOwnedByClient)) => ObserverOwner.Client,
            Type t when t.IsAssignableTo(typeof(IAmOwnedByKernel)) => ObserverOwner.Kernel,
            _ => ObserverOwner.None
        };

    /// <summary>
    /// Stops all non-replay observer jobs that are currently preparing or running so they can be resumed when the observer reconnects.
    /// Replay jobs are excluded because they are managed independently of the observer's subscription lifecycle.
    /// </summary>
    async Task PauseJobs()
    {
        var allJobs = await _jobsManager.GetUnfinishedJobs();

        // Explicitly do not pause replay jobs.
        var pauseTasks = allJobs
            .Where(job => job is { Request: IObserverJobRequest observerJobRequest } &&
                          observerJobRequest is not ReplayObserverRequest &&
                          ShouldPauseJob(job.Status) &&
                          observerJobRequest.ObserverKey == _observerKey)
            .Select(job => _jobsManager.Stop(job.Id));
        await Task.WhenAll(pauseTasks);
        return;

        static bool ShouldPauseJob(JobStatus status) => status is JobStatus.Running or JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps;
    }

    async Task RecoverAfterSubscribing(bool leavesQuarantine)
    {
        if (leavesQuarantine && IsQuarantined)
        {
            // The leave was only scheduled because quarantine entry is still stopping its retry work. The new
            // subscription is in place, so recover once the leave has actually entered Disconnected.
            _recoverSubscriptionAfterQuarantine = true;
            _retryRecoveryAfterQuarantine = true;
            return;
        }

        await RecoverSubscribedObserver();
    }

    Task RecoverAfterQuarantine()
    {
        // Recovery only schedules the transition out of Disconnected when called from its entry hook. That transition
        // runs after Disconnected is persisted, and is dropped if the write fails, after this method has returned.
        // Keep the recovery owed until a settled state is entered, so the operator's next clearance retries it.
        _retryRecoveryAfterQuarantine = true;
        return RecoverSubscribedObserver();
    }

    /// <summary>
    /// Cancels any recovery owed after a quarantine ended, including one that is running right now.
    /// </summary>
    /// <remarks>
    /// Unsubscribing, a subscription that may end quarantine, and wiping the world quarantine belonged to all replace
    /// the subscription that recovery was for. A recovery entered from the <see cref="Disconnected"/> entry hook can
    /// be running in an interleaved call while one of them happens, so moving the generation on is what stops it from
    /// resuming jobs, retrying partitions or starting catch-up for a subscription that is no longer there.
    /// </remarks>
    void SupersedeRecoveryAfterQuarantine()
    {
        _recoverSubscriptionAfterQuarantine = false;
        _retryRecoveryAfterQuarantine = false;
        _owedRecoveryAttempts = 0;
        _recoveryGeneration++;
    }

    /// <summary>
    /// Retries a recovery owed after a quarantine ended that never reached a settled state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task RetryRecoveryAfterQuarantine()
    {
        switch (await GetCurrentState())
        {
            case Disconnected:
                await RecoverAfterQuarantine();
                break;

            case Routing or CatchingUpInFlight:
                // Stranded in a transient state whose onward transition was dropped. Disconnected owns recovery.
                _recoverSubscriptionAfterQuarantine = true;
                await TransitionTo<Disconnected>();
                break;
        }
    }

    /// <summary>
    /// Gets whether a recovery has been overtaken since it started, so it must not act any further.
    /// </summary>
    /// <param name="recovery">The generation the recovery captured when it started.</param>
    /// <returns>True if the recovery must stop, false if it may carry on.</returns>
    bool IsRecoverySuperseded(int recovery) =>
        IsQuarantined || !_subscription.IsSubscribed || recovery != _recoveryGeneration;

    async Task RecoverSubscribedObserver()
    {
        // A newer recovery, an unsubscription or a new subscription supersedes this one. Every await below can let
        // one of those run in between, so the recovery rechecks before each step and before its onward transition.
        var recovery = ++_recoveryGeneration;
        _recoveriesInProgress++;
        try
        {
            if (await TransitionToReplayIfNeeded(recovery))
            {
                return;
            }

            await ResumeJobs(recovery);
            if (IsRecoverySuperseded(recovery))
            {
                return;
            }

            // Recovering failed partitions starts one job per partition through the jobs manager. An observer
            // that has accumulated hundreds of them - a reactor whose handler was broken for a week - spends
            // longer than the caller's 30 second grain-call budget in that loop, so the Subscribe never
            // returned: the client timed out, retried, and the observer was recorded as never subscribed.
            // Subscribing is about wiring the subscriber up; recovery is work the observer owes afterwards, in
            // bounded turns of its own that repeated subscribes do not multiply - see Observer.PartitionRecovery.cs.
            // Clearing quarantine on a subscribed observer owes the same recovery.
            await TryRecoverAllFailedPartitions();
            SeedSuspectedSkipOwnership();
            await TransitionTo<CatchingUpInFlight>();
        }
        finally
        {
            _recoveriesInProgress--;
        }
    }

    async Task ResumeJobs(int recovery)
    {
        if (IsRecoverySuperseded(recovery))
        {
            return;
        }

        var unfilteredJobs = await _jobsManager.GetUnfinishedJobs();
        if (IsRecoverySuperseded(recovery))
        {
            return;
        }

        // Explicitly do not resume replay jobs.
        var resumeTasks = unfilteredJobs
            .Where(job => job is { Request: IObserverJobRequest observerJobRequest } &&
                          observerJobRequest is not ReplayObserverRequest &&
                          ShouldResumeJob(job.Status) &&
                          observerJobRequest.ObserverKey == _subscription.ObserverKey)
            .Select(job => IsRecoverySuperseded(recovery)
                ? Task.CompletedTask
                : _jobsManager.Resume(job.Id));
        await Task.WhenAll(resumeTasks);
        return;

        static bool ShouldResumeJob(JobStatus status) => status is not JobStatus.Failed and not JobStatus.CompletedSuccessfully
            and not JobStatus.CompletedWithFailures and not JobStatus.Removing;
    }

    async Task RemoveReminder(FailedPartition partition)
    {
        await UnregisterReminderNamed(PartitionReminderName(partition.Id));
        if (partition.Partition.ToString() != AlertReminderName)
        {
            await UnregisterReminderNamed(partition.Partition.ToString());
        }
    }

    async Task UnregisterReminderNamed(string reminderName)
    {
        var reminder = await this.GetReminder(reminderName);
        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder);
        }
    }

    class WriteSuspension : IDisposable
    {
        readonly Observer _observer;

        public WriteSuspension(Observer observer)
        {
            _observer = observer;
            _observer._stateWritingSuspended = true;
        }

        public void Dispose() => _observer._stateWritingSuspended = false;
    }
}
