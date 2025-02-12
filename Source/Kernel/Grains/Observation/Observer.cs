// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Applications.Orleans.StateMachines;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Configuration;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Grains.Observation.States;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserver"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observer"/> class.
/// </remarks>
/// <param name="failures"><see cref="IPersistentState{T}"/> for failed partitions.</param>
/// <param name="replayStateServiceClient"><see cref="IObserverServiceClient"/> for notifying about replay to all silos.</param>
/// <param name="configurationProvider">The <see cref="IConfigurationForObserverProvider"/> for getting the <see cref="Observers"/> configuration.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="meter"><see cref="Meter{T}"/> for the observer.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Observers)]
public partial class Observer(
    [PersistentState(nameof(FailedPartition), WellKnownGrainStorageProviders.FailedPartitions)]
    IPersistentState<FailedPartitions> failures,
    IObserverServiceClient replayStateServiceClient,
    IConfigurationForObserverProvider configurationProvider,
    ILogger<Observer> logger,
    IMeter<Observer> meter,
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

    /// <inheritdoc/>
    protected override Type InitialState => typeof(Routing);

    FailedPartitions Failures => failures.State;

    /// <inheritdoc/>
    public override async Task OnActivation(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.MaxValue);

        _observerKey = ObserverKey.Parse(this.GetPrimaryKeyString());
        _observerId = _observerKey.ObserverId;

        _jobsManager = GrainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_observerKey.EventStore, _observerKey.Namespace));

        await failures.ReadStateAsync();

        _eventSequence = GrainFactory.GetGrain<IEventSequence>(
            new EventSequenceKey(_observerKey.EventSequenceId, _observerKey.EventStore, _observerKey.Namespace));

        var eventSequenceKey = new EventSequenceKey(_observerKey.EventSequenceId, _observerKey.EventStore, _observerKey.Namespace);
        _appendedEventsQueues = GrainFactory.GetGrain<IAppendedEventsQueues>(eventSequenceKey);
        _metrics = meter.BeginObserverScope(_observerId, _observerKey);
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await TransitionTo<Disconnected>();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

#pragma warning disable CA1721 // Property names should not match get methods
    /// <inheritdoc/>
    public Task<ObserverState> GetState() => Task.FromResult(State);
#pragma warning restore CA1721 // Property names should not match get methods

    /// <inheritdoc/>
    public Task<ObserverSubscription> GetSubscription() => Task.FromResult(_subscription);

    /// <inheritdoc/>
    public Task<bool> IsSubscribed() => Task.FromResult(_subscription.IsSubscribed);

    /// <inheritdoc/>
    public Task<IEnumerable<EventType>> GetEventTypes() => Task.FromResult(State.EventTypes);

    /// <inheritdoc/>
    public async Task Subscribe<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = null)
        where TObserverSubscriber : IObserverSubscriber
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        logger.Subscribing();

        State = State with { Type = type, EventTypes = eventTypes };

        _subscription = new ObserverSubscription(
            _observerId,
            _observerKey,
            eventTypes,
            typeof(TObserverSubscriber),
            siloAddress,
            subscriberArgs);
        await WriteStateAsync();
        await ResumeJobs();
        await TryRecoverAllFailedPartitions();
        await TransitionTo<Routing>();
    }

    /// <inheritdoc/>
    public override IImmutableList<IState<ObserverState>> CreateStates() => new IState<ObserverState>[]
    {
        new Disconnected(),

        new Routing(
            _observerKey,
            new ReplayEvaluator(
                GrainFactory,
                _observerKey.EventStore,
                _observerKey.Namespace),
            _eventSequence,
            loggerFactory.CreateLogger<Routing>()),

        new ResumeReplay(
            _observerKey,
            replayStateServiceClient,
            _jobsManager),

        new Replay(
            _observerKey,
            replayStateServiceClient,
            _jobsManager,
            loggerFactory.CreateLogger<Replay>()),

        new Observing(
            _appendedEventsQueues,
            _observerKey.EventStore,
            _observerKey.Namespace,
            _observerKey.EventSequenceId,
            loggerFactory.CreateLogger<Observing>())
    }.ToImmutableList();

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        _subscription = ObserverSubscription.Unsubscribed;
        await TransitionTo<Disconnected>();
    }

    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        await RemoveReminder(reminderName);
        if (!_subscription.IsSubscribed)
        {
            return;
        }

        var partition = failures.State.Partitions.FirstOrDefault(_ => _.Partition.ToString() == reminderName);
        if (partition is not null)
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

    /// <inheritdoc/>
    protected override Task OnBeforeEnteringState(IState<ObserverState> state)
    {
        if (state is BaseObserverState observerState)
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
    }

    async Task ResumeJobs()
    {
        // TODO: Do this in a Task.WhenAll because JobsManager is reentrant now
        var unfilteredJobs = await _jobsManager.GetAllJobs();
        foreach (var job in unfilteredJobs.Where(job => job is { Request: IObserverJobRequest, IsResumable: true }).ToArray())
        {
            await _jobsManager.Resume(job.Id);
        }
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
