// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Placement;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.StateMachines;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
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
    ILoggerFactory loggerFactory) : StateMachine<ObserverState>, IObserver, IRemindable
{
    ObserverId _observerId = ObserverId.Unspecified;
    ObserverKey _observerKey = ObserverKey.NotSet;
    ObserverSubscription _subscription = ObserverSubscription.Unsubscribed;
    IJobsManager _jobsManager = null!;
    bool _stateWritingSuspended;
    IEventSequence _eventSequence = null!;
    IAppendedEventsQueues _appendedEventsQueues = null!;
    IMeterScope<Observer>? _metrics;
    bool _isPreparingCatchup;
    Dictionary<EventType, EventTypeSchema> _eventTypeSchemas = [];
    int _statePersistenceBatchInterval = 1;
    int _debouncedProgressWrites;

    /// <inheritdoc/>
    protected override Type InitialState => typeof(Routing);

    ObserverDefinition Definition => observerDefinition.State;

    FailedPartitions Failures => failures.State;

    /// <summary>
    /// Gets the <see cref="ObserverRunningState"/> of the state the observer is currently in.
    /// </summary>
    /// <remarks>
    /// Every decision the observer makes about itself - the delivery gate above all - reads this rather than the
    /// reported <see cref="ObserverState.RunningState"/>. The reported value deliberately holds the last settled
    /// running state while the observer passes through a transitional state, so reading it from inside the observer
    /// would let a gate act on a running state the observer has already left. This value follows the state machine
    /// exactly, which is what the reported value used to do before it was decoupled.
    /// </remarks>
    ObserverRunningState CurrentRunningState =>
        CurrentState is BaseObserverState state ? state.RunningState : ObserverRunningState.Unknown;

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
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await FlushDebouncedProgressState();
        if (reason.ReasonCode != DeactivationReasonCode.ShuttingDown)
        {
            await TransitionTo<Disconnected>();
            await base.OnDeactivateAsync(reason, cancellationToken);
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
    public Task<bool> IsObserverQuarantined() => Task.FromResult(CurrentRunningState == ObserverRunningState.Quarantined);

    /// <inheritdoc/>
    public Task<IEnumerable<Key>> GetFailedPartitionKeys() => Task.FromResult(Failures.Partitions.Select(p => p.Partition));

    /// <inheritdoc/>
    public async Task ClearObserverQuarantine()
    {
        if (CurrentRunningState == ObserverRunningState.Quarantined)
        {
            await TransitionTo<Routing>();
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventType>> GetEventTypes() => Task.FromResult(Definition.EventTypes);

    /// <inheritdoc/>
    public async Task Subscribe<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = null,
        bool isReplayable = true,
        ObserverFilters? filters = null)
        where TObserverSubscriber : IObserverSubscriber
    {
        var owner = GetOwner<TObserverSubscriber>();

        var eventTypeSchemas = await storage.GetEventStore(_observerKey.EventStore).EventTypes.GetFor(eventTypes);
        _eventTypeSchemas = eventTypeSchemas.ToDictionary(s => s.Type);

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        // Re-read all persistent state from storage. When the silo is shared
        // across tests (KeepAlive grains survive ForceActivationCollection),
        // the in-memory state may be stale if databases were dropped between
        // tests. Reading from storage detects this and resets to defaults.
        await ReadStateAsync();
        await observerDefinition.ReadStateAsync();
        await failures.ReadStateAsync();

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

        if (CurrentRunningState == ObserverRunningState.Quarantined)
        {
            return;
        }

        if (await TransitionToReplayIfNeeded())
        {
            return;
        }
        await ResumeJobs();
        await TryRecoverAllFailedPartitions();
        await TransitionTo<CatchingUpInFlight>();
    }

    /// <inheritdoc/>
    public async Task SubscribeToAllEvents<TObserverSubscriber>(
        ObserverType type,
        SiloAddress siloAddress,
        object? subscriberArgs = null,
        bool isReplayable = true)
        where TObserverSubscriber : IObserverSubscriber
    {
        var owner = GetOwner<TObserverSubscriber>();

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        logger.Subscribing();
        logger.SubscribingToAllEvents();

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

        if (CurrentRunningState == ObserverRunningState.Quarantined)
        {
            return;
        }

        if (await TransitionToReplayIfNeeded())
        {
            return;
        }
        await ResumeJobs();
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
        await PauseJobs();
        _subscription = ObserverSubscription.Unsubscribed;
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
        await RemoveReminder(reminderName);
        if (CurrentRunningState == ObserverRunningState.Quarantined)
        {
            return;
        }

        if (!_subscription.IsSubscribed)
        {
            return;
        }

        var partition = failures.State.Partitions.FirstOrDefault(_ => _.Partition.ToString() == reminderName);
        if (partition is { IsQuarantined: false })
        {
            await StartRecoverJobForFailedPartition(partition);
        }
    }

    /// <summary>
    /// Set subscription explicitly, without subscribing. This method is internal and visible to the test suite and only meant to be used with testing.
    /// </summary>
    /// <param name="subscription">Subscription to set.</param>
    internal void SetSubscription(ObserverSubscription subscription)
    {
        _subscription = subscription;
    }

    /// <summary>
    /// Removes all reminders for currently failed partitions.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task RemoveFailedPartitionReminders()
    {
        foreach (var partition in Failures.Partitions.Select(_ => _.Partition))
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
        var jobs = await _jobsManager.GetAllJobs();
        var stopTasks = jobs
            .Where(_ => _.Request is RetryFailedPartitionRequest request && request.ObserverKey == _observerKey)
            .Select(_ => _jobsManager.Stop(_.Id));
        await Task.WhenAll(stopTasks);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Only a settled state is mirrored onto the reported <see cref="ObserverState.RunningState"/>. A transitional
    /// state has no running state of its own and answers <see cref="ObserverRunningState.Unknown"/>; mirroring that
    /// would publish and persist it, because the transition writes the state before the transition it schedules from
    /// its own OnEnter gets to run. Everything watching an observer - the change stream behind
    /// <see cref="Storage.Observation.IObserverStateStorage.ObserveAll"/>, the streaming gRPC and REST surfaces on
    /// top of it, and <see cref="GetState"/>, which interleaves and so is answered mid-transition - would see a
    /// healthy observer flap to Unknown and back on every route, catch-up completion and watchdog re-route. Holding
    /// the last settled value across the transition is what makes the reported running state stable. An observer
    /// that has never settled keeps reporting Unknown, which is exactly what it is.
    /// </remarks>
    protected override Task OnBeforeEnteringState(IState<ObserverState> state)
    {
        if (state is BaseObserverState { IsTransitional: false } observerState)
        {
            State = State with { RunningState = observerState.RunningState };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task WriteStateAsync()
    {
        if (_stateWritingSuspended) return;
        await base.WriteStateAsync();

        // Any actual persist carries the observer's current NextEventSequenceNumber, so it flushes whatever
        // progress-only advance was being debounced. Resetting the counter keeps the debounce window bounded by
        // the most recent write from any source, not only the progress-only path.
        _debouncedProgressWrites = 0;
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
        var allJobs = await _jobsManager.GetAllJobs();

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
        var unfilteredJobs = await _jobsManager.GetAllJobs();

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

    async Task RemoveReminder(Key partition)
    {
        var reminder = await this.GetReminder(partition.ToString());
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
