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
    ILoggerFactory loggerFactory) : StateMachine<ObserverState>, IObserver, IRemindable, IDisposable
{
    readonly HashSet<JobId> _concludedCatchUpJobs = [];

    /// <summary>
    /// The catch-up job acquisition currently in flight, if any.
    /// </summary>
    /// <remarks>
    /// A job being started is not listed by the jobs manager until its start completes, so a catch-up arriving in the
    /// meantime - routing after an interleaved <see cref="CaughtUp"/>, or the appended-events queue triggering one -
    /// would find no owner and start a second job over the same events. It adopts the outcome of this acquisition instead.
    /// </remarks>
    Task<JobId>? _pendingCatchUpAcquisition;

    ObserverId _observerId = ObserverId.Unspecified;
    ObserverKey _observerKey = ObserverKey.NotSet;
    ObserverSubscription _subscription = ObserverSubscription.Unsubscribed;
    IJobsManager _jobsManager = null!;
    bool _stateWritingSuspended;
    bool _resumingQuarantine;
    bool _subscriptionSetupFailed;

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
    /// Counts every time catch-up ownership started moving - a handover entering or an acquisition starting.
    /// </summary>
    /// <remarks>
    /// A handover can both start and finish while the watchdog awaits its job lookup, leaving nothing in flight to
    /// see afterwards and a listing that predates the successor it started. A changed epoch is what reveals it.
    /// </remarks>
    int _catchUpOwnershipEpoch;
    int _catchupRecoveryAttempts;
    Dictionary<EventType, EventTypeSchema> _eventTypeSchemas = [];
    int _statePersistenceBatchInterval = 1;
    int _debouncedProgressWrites;

    /// <inheritdoc/>
    protected override Type InitialState => typeof(Routing);

    ObserverDefinition Definition => observerDefinition.State;

    FailedPartitions Failures => failures.State;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        ScheduleAlertReport();
    }

    /// <inheritdoc/>
    public override async Task OnActivation(CancellationToken cancellationToken)
    {
        _observerKey = ObserverKey.Parse(this.GetPrimaryKeyString());
        _observerId = _observerKey.ObserverId;

        _jobsManager = GrainFactory.GetJobsManager(_observerKey.EventStore, _observerKey.Namespace);

        await failures.ReadStateAsync();

        _eventSequence = GrainFactory.GetGrain<IEventSequence>(
            new EventSequenceKey(_observerKey.EventSequenceId, _observerKey.EventStore, _observerKey.Namespace));

        var eventSequenceKey = new EventSequenceKey(_observerKey.EventSequenceId, _observerKey.EventStore, _observerKey.Namespace);
        _appendedEventsQueues = GrainFactory.GetGrain<IAppendedEventsQueues>(eventSequenceKey);
        _metrics = meter.BeginObserverScope(_observerId, _observerKey);

        var config = await configurationProvider.GetFor(_observerKey);
        _statePersistenceBatchInterval = config.StatePersistenceBatchInterval < 1 ? 1 : config.StatePersistenceBatchInterval;
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
    public Task<bool> IsPreparingCatchup() => Task.FromResult(_isPreparingCatchup);

    /// <inheritdoc/>
    public Task<bool> HasFailedPartitions() => Task.FromResult(Failures.HasFailedPartitions);

    /// <inheritdoc/>
    public Task<bool> IsObserverQuarantined() => Task.FromResult(State.RunningState == ObserverRunningState.Quarantined);

    /// <inheritdoc/>
    public Task<IEnumerable<Key>> GetFailedPartitionKeys() => Task.FromResult(Failures.Partitions.Select(p => p.Partition));

    /// <inheritdoc/>
    public async Task ClearObserverQuarantine()
    {
        ThrowIfSealed();
        if (State.RunningState == ObserverRunningState.Quarantined)
        {
            // With nobody subscribed this is the routing pass every activation of an unsubscribed observer already runs, so it drops nothing a plain reactivation would not.
            await ReviveFromQuarantine();
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
        => SubscribeToEventTypes<TObserverSubscriber>(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, reactivateRetired: reactivateRetired, automatic: automatic);

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
        var owner = GetOwner<TObserverSubscriber>();

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        logger.Subscribing();
        logger.SubscribingToAllEvents();

        await ReadStateAsync();
        await observerDefinition.ReadStateAsync();
        await failures.ReadStateAsync();
        if (!reactivateRetired && State.AlertDisposition == AlertDisposition.Retired) return;
        await BeginAlertLifecycle();
        await LeaveQuarantineForSubscription();

        observerDefinition.State = observerDefinition.State with
        {
            Type = type,
            Owner = owner,
            EventTypes = [],
            IsReplayable = isReplayable
        };
        await observerDefinition.WriteStateAsync();

        _subscription = new(
            _observerId,
            _observerKey,
            [],
            typeof(TObserverSubscriber),
            siloAddress,
            subscriberArgs,
            isReplayable);

        State = State with { SubscribesToAllEvents = true };
        await WriteStateAsync();

        if (await TransitionToReplayIfNeeded())
        {
            return;
        }
        await ResumeJobs();

        // Recovering failed partitions starts one job per partition through the jobs manager. An observer
        // that has accumulated hundreds of them - a reactor whose handler was broken for a week - spends
        // longer than the caller's 30 second grain-call budget in that loop, so the Subscribe never
        // returned: the client timed out, retried, and the observer was recorded as never subscribed.
        // Subscribing is about wiring the subscriber up; recovery is work the observer owes afterwards, in
        // bounded turns of its own that repeated subscribes do not multiply - see Observer.PartitionRecovery.cs.
        await TryRecoverAllFailedPartitions();
        await TransitionTo<CatchingUpInFlight>();
    }

    /// <inheritdoc/>
    public override IImmutableList<IState<ObserverState>> CreateStates() => new IState<ObserverState>[]
    {
        new Disconnected(),

        new Routing(
            _observerKey,
            observerDefinition,
            _eventSequence,
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
        _subscription = ObserverSubscription.Unsubscribed;
        await PauseJobs();
        await TransitionTo<Disconnected>();
    }

    /// <inheritdoc/>
    public Task UnsubscribeIfMatchesClient(ConnectionId connectionId)
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
                return Task.CompletedTask;
            }

            if (remaining.Length > 0)
            {
                _subscription = _subscription with
                {
                    SiloAddress = remaining[0].SiloAddress,
                    Arguments = remaining[0].ConnectedClient,
                    Targets = remaining
                };
                return Task.CompletedTask;
            }

            return Unsubscribe();
        }

        if (_subscription.IsSubscribed &&
            _subscription.Arguments is ConnectedClient connectedClient &&
            connectedClient.ConnectionId != connectionId)
        {
            return Task.CompletedTask;
        }

        return Unsubscribe();
    }

    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName == AlertReminderName)
        {
            await ReconcileAlertsIfNeeded();
            return;
        }

        await UnregisterReminderNamed(reminderName);
        if (IsRetired || _removed || State.RunningState == ObserverRunningState.Quarantined)
        {
            return;
        }

        if (!_subscription.IsSubscribed)
        {
            return;
        }

        // Accept raw names registered by main as well as bounded episode-specific retry names.
        foreach (var partition in Failures.Partitions.Where(partition => !partition.IsQuarantined &&
            (PartitionReminderName(partition.Id) == reminderName || partition.Partition.ToString() == reminderName)))
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
        await _alertMutationLock.WaitAsync();
        try
        {
            if (state is BaseObserverState observerState)
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

                State = State with { RunningState = observerState.RunningState };
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    /// <inheritdoc/>
    protected override async Task WriteStateAsync()
    {
        if (_stateWritingSuspended || !_observerExists) return;
        await _stateWriteLock.WaitAsync();
        try
        {
            if (_removed || _stateWritingSuspended || !_observerExists) return;

            // A state-machine OnEnter can return an earlier record after awaiting a job callback. Preserve the
            // source-owned metadata advanced by that callback, and serialize writes from AlwaysInterleave methods.
            State = State with
            {
                AlertLifecycleId = _alertLifecycleId,
                AlertRevision = _alertRevision,
                AlertDisposition = _alertDisposition,
                QuarantineEpisodeId = _quarantineEpisodeId
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
        bool automatic = false)
        where TObserverSubscriber : IObserverSubscriber
    {
        // Automatic kernel subscription cannot revive a retired observer or a sealed activation.
        // Ordinary Subscribe keeps its explicit-registration semantics, including throwing when sealed.
        if (additive && (_removed || IsRetired)) return;
        ThrowIfSealed();
        if (recovering && !await NeedsSubscriptionRecovery(eventTypes)) return;

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

            // Ordinary subscription re-reads storage, including after a shared test silo's database reset.
            // Recovery must retain live progress rather than reload a stale persisted position.
            if (!recovering)
            {
                await ReadStateAsync();
                await observerDefinition.ReadStateAsync();
                await failures.ReadStateAsync();
            }

            if (!reactivateRetired && State.AlertDisposition == AlertDisposition.Retired) return;
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

            await SetUpSubscription<TObserverSubscriber>(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, automatic);

            // A persisted Active marker alone does not prove setup completed after an entry-write failure.
            _subscriptionSetupFailed = await GetCurrentState() is not (Observing or States.Replay);
        }
        catch
        {
            _subscriptionSetupFailed = true;
            throw;
        }
    }

    async Task SetUpSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        ObserverFilters? filters,
        bool automatic = false)
        where TObserverSubscriber : IObserverSubscriber
    {
        var owner = GetOwner<TObserverSubscriber>();
        logger.Subscribing();
        logger.SubscribingWithEventTypes(eventTypes.Count(), string.Join(", ", eventTypes.Select(et => et.Id)));

        observerDefinition.State = observerDefinition.State with
        {
            Type = type,
            Owner = owner,
            EventTypes = eventTypes,
            IsReplayable = isReplayable
        };
        await observerDefinition.WriteStateAsync();

        if (subscriberArgs is ConnectedClient connectedClient)
        {
            var target = new ObserverSubscriberTarget(siloAddress, connectedClient);
            if (CanFanOutInto<TObserverSubscriber>(eventTypes, filters))
            {
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
            }
            else
            {
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
                    Targets = [target]
                };
            }
        }
        else
        {
            _subscription = new(
                _observerId,
                _observerKey,
                eventTypes,
                typeof(TObserverSubscriber),
                siloAddress,
                subscriberArgs,
                isReplayable,
                filters);
        }

        State = State with { SubscribesToAllEvents = false };
        await WriteStateAsync();

        if (automatic && State.RunningState == ObserverRunningState.Quarantined)
        {
            return;
        }

        if (await TransitionToReplayIfNeeded(automatic))
        {
            return;
        }
        await ResumeJobs();
        if (automatic && State.RunningState == ObserverRunningState.Quarantined)
        {
            return;
        }

        // Recovering failed partitions starts one job per partition through the jobs manager. An observer
        // that has accumulated hundreds of them - a reactor whose handler was broken for a week - spends
        // longer than the caller's 30 second grain-call budget in that loop, so the Subscribe never
        // returned: the client timed out, retried, and the observer was recorded as never subscribed.
        // Subscribing is about wiring the subscriber up; recovery is work the observer owes afterwards, in
        // bounded turns of its own that repeated subscribes do not multiply - see Observer.PartitionRecovery.cs.
        await TryRecoverAllFailedPartitions();
        await TransitionTo<CatchingUpInFlight>();
    }

    bool CanFanOutInto<TObserverSubscriber>(IEnumerable<EventType> eventTypes, ObserverFilters? filters)
        where TObserverSubscriber : IObserverSubscriber =>
        _subscription.IsSubscribed &&
        _subscription.Targets.Count > 0 &&
        _subscription.SubscriberType == typeof(TObserverSubscriber) &&
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

    async Task ResumeJobs()
    {
        var unfilteredJobs = await _jobsManager.GetUnfinishedJobs();

        // Explicitly do not resume replay jobs.
        var resumeTasks = unfilteredJobs
            .Where(job => job is { Request: IObserverJobRequest observerJobRequest } &&
                          observerJobRequest is not ReplayObserverRequest &&
                          ShouldResumeJob(job.Status) &&
                          observerJobRequest.ObserverKey == _subscription.ObserverKey)
            .Select(job => _jobsManager.Resume(job.Id));
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
