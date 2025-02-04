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
public class Observer(
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
    public Task SetHandledStats(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        State = State with
        {
            LastHandledEventSequenceNumber = lastHandledEventSequenceNumber
        };

        return WriteStateAsync();
    }

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

        State = State with { Type = type };

        _subscription = new ObserverSubscription(
            _observerId,
            _observerKey,
            eventTypes,
            typeof(TObserverSubscriber),
            siloAddress,
            subscriberArgs);

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
    public async Task Replay()
    {
        if (State.RunningState == ObserverRunningState.Active)
        {
            await TransitionTo<Replay>();
        }
    }

    /// <inheritdoc/>
    public Task ReplayPartition(Key partition) => ReplayPartitionTo(partition, EventSequenceNumber.Max);

    /// <inheritdoc/>
    public async Task ReplayPartitionTo(Key partition, EventSequenceNumber sequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.AttemptReplayPartition(partition, sequenceNumber);
        await _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(
            JobId.New(),
            new(
                _observerKey,
                _subscription,
                partition,
                EventSequenceNumber.First,
                sequenceNumber,
                State.EventTypes));

        State.ReplayingPartitions.Add(partition);
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task Replayed(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await TransitionTo<Routing>();
    }

    /// <inheritdoc/>
    public async Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FinishedReplayForPartition(partition);
        State.ReplayingPartitions.Remove(partition);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await StartCatchupJobIfNeeded(partition, lastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    public async Task PartitionFailed(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        _metrics?.PartitionFailed(partition);
        logger.PartitionFailed(partition, sequenceNumber);
        var failure = failures.State.RegisterAttempt(partition, sequenceNumber, exceptionMessages, exceptionStackTrace);
        var config = await configurationProvider.GetFor(_observerKey);
        if (config.MaxRetryAttempts == 0 || failure.Attempts.Count() <= config.MaxRetryAttempts)
        {
            await this.RegisterOrUpdateReminder(partition.ToString(), GetNextRetryDelay(failure, config), TimeSpan.FromHours(48));
        }
        else
        {
            logger.GivingUpOnRecoveringFailedPartition(partition);
        }

        await failures.WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FailingPartitionRecovered(partition);
        failures.State.Remove(partition);
        await failures.WriteStateAsync();
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await StartCatchupJobIfNeeded(partition, lastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    public async Task FailedPartitionPartiallyRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FailingPartitionPartiallyRecovered(partition, lastHandledEventSequenceNumber);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task CatchUp()
    {
        _isPreparingCatchup = true;
        using var scope = logger.BeginObserverScope(State.Id, _observerKey);

        var subscription = await GetSubscription();

        var jobs = await _jobsManager.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>();
        var jobsForThisObserver = jobs.Where(IsJobForThisObserver);
        if (jobs.Any(_ => _.Status == JobStatus.Running))
        {
            logger.FinishingExistingCatchUpJob();
            return;
        }

        var pausedJob = jobs.FirstOrDefault(_ => _.Status == JobStatus.Paused);

        if (pausedJob is not null)
        {
            logger.ResumingCatchUpJob();
            await _jobsManager.Resume(pausedJob.Id);
        }
        else
        {
            logger.StartCatchUpJob(State.NextEventSequenceNumber);
            await _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(
                JobId.New(),
                new(
                    _observerKey,
                    subscription,
                    State.NextEventSequenceNumber,
                    State.EventTypes));
        }
    }

    /// <inheritdoc/>
    public async Task RegisterCatchingUpPartitions(IEnumerable<Key> partitions)
    {
        using var scope = logger.BeginObserverScope(State.Id, _observerKey);
        logger.RegisteringCatchingUpPartitions();
        foreach (var partition in partitions)
        {
            State.CatchingUpPartitions.Add(partition);
        }

        await WriteStateAsync();

        _isPreparingCatchup = false;
    }

    /// <inheritdoc/>
    public async Task CaughtUp(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await TransitionTo<Routing>();
    }

    /// <inheritdoc/>
    public async Task PartitionCaughtUp(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.PartitionCaughtUp(partition, lastHandledEventSequenceNumber);
        State.CatchingUpPartitions.Remove(partition);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await StartCatchupJobIfNeeded(partition, lastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    public async Task TryStartRecoverJobForFailedPartition(Key partition)
    {
        if (!Failures.TryGet(partition, out var failure))
        {
            return;
        }

        await StartRecoverJobForFailedPartition(failure);
    }

    /// <inheritdoc/>
    public async Task TryRecoverAllFailedPartitions()
    {
        foreach (var partition in Failures.Partitions)
        {
            await StartRecoverJobForFailedPartition(partition);
        }
    }

    /// <inheritdoc/>
    public async Task Handle(Key partition, IEnumerable<AppendedEvent> events)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        if (!ShouldHandleEvent(partition))
        {
            return;
        }

        if (!events.Any(_ => _subscription.EventTypes.Contains(_.Metadata.Type)))
        {
            State = State with { NextEventSequenceNumber = events.Last().Metadata.SequenceNumber.Next() };
            await WriteStateAsync();
            return;
        }

        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var tailEventSequenceNumber = State.NextEventSequenceNumber;

        var eventsToHandle = events.Where(_ => _.Metadata.SequenceNumber >= tailEventSequenceNumber).ToArray();
        var numEventsSuccessfullyHandled = EventCount.Zero;
        var stateChanged = false;
        if (eventsToHandle.Length != 0)
        {
            using (new WriteSuspension(this))
            {
                try
                {
                    var key = new ObserverSubscriberKey(
                        _observerKey.ObserverId,
                        _observerKey.EventStore,
                        _observerKey.Namespace,
                        _observerKey.EventSequenceId,
                        partition,
                        _subscription.SiloAddress.ToParsableString());

                    var firstEvent = eventsToHandle[0];

                    var subscriber = (GrainFactory.GetGrain(_subscription.SubscriberType, key) as IObserverSubscriber)!;
                    tailEventSequenceNumber = firstEvent.Metadata.SequenceNumber;
                    var result = await subscriber.OnNext(partition, eventsToHandle, new(_subscription.Arguments));
                    numEventsSuccessfullyHandled = result.HandledAnyEvents
                        ? eventsToHandle.Count(_ => _.Metadata.SequenceNumber <= result.LastSuccessfulObservation)
                        : EventCount.Zero;

                    if (result.State == ObserverSubscriberState.Failed)
                    {
                        failed = true;
                        exceptionMessages = result.ExceptionMessages;
                        exceptionStackTrace = result.ExceptionStackTrace;
                        tailEventSequenceNumber = result.HandledAnyEvents
                            ? result.LastSuccessfulObservation
                            : firstEvent.Metadata.SequenceNumber;
                    }
                    else if (result.State == ObserverSubscriberState.Disconnected)
                    {
                        await Unsubscribe();
                        stateChanged = true;
                    }

                    if (numEventsSuccessfullyHandled > 0)
                    {
                        stateChanged = true;
                        State = State with { NextEventSequenceNumber = result.LastSuccessfulObservation.Next() };
                        var previousLastHandled = State.LastHandledEventSequenceNumber;
                        var shouldSetLastHandled =
                            previousLastHandled == EventSequenceNumber.Unavailable ||
                            previousLastHandled < result.LastSuccessfulObservation;
                        State = State with
                        {
                            LastHandledEventSequenceNumber = shouldSetLastHandled
                                ? result.LastSuccessfulObservation
                                : previousLastHandled,
                        };
                    }
                }
                catch (Exception ex)
                {
                    failed = true;
                    exceptionMessages = ex.GetAllMessages().ToArray();
                    exceptionStackTrace = ex.StackTrace ?? string.Empty;
                }
            }

            try
            {
                if (failed)
                {
                    await PartitionFailed(partition, tailEventSequenceNumber, exceptionMessages, exceptionStackTrace);
                }
                else
                {
                    _metrics?.SuccessfulObservation();
                }

                if (stateChanged)
                {
                    await WriteStateAsync();
                }
            }
            catch (Exception ex)
            {
                logger.ObserverFailedForUnknownReasonsAfterHandlingEvents(ex);
            }
        }
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

    static TimeSpan GetNextRetryDelay(FailedPartition failure, Observers config)
    {
        var time = TimeSpan.FromSeconds(config.BackoffDelay * Math.Pow(config.ExponentialBackoffDelayFactor, failure.Attempts.Count()));
        var maxTime = TimeSpan.FromSeconds(config.MaximumBackoffDelay);

        if (time > maxTime)
        {
            return maxTime;
        }

        if (time == TimeSpan.Zero)
        {
            return TimeSpan.FromSeconds(config.BackoffDelay);
        }

        return time;
    }

    bool ShouldHandleEvent(Key partition)
    {
        if (!_subscription.IsSubscribed)
        {
            logger.ObserverIsNotSubscribed();
            return false;
        }

        if (Failures.IsFailed(partition))
        {
            logger.PartitionIsFailed(partition);
            return false;
        }

        if (State.RunningState != ObserverRunningState.Active)
        {
            logger.ObserverIsNotActive();
            return false;
        }

        if (_isPreparingCatchup)
        {
            logger.ObserverIsPreparingCatchup();
            return false;
        }

        if (State.ReplayingPartitions.Contains(partition))
        {
            logger.PartitionReplayingCannotHandleNewEvents(partition);
            return false;
        }

        if (State.CatchingUpPartitions.Contains(partition))
        {
            logger.PartitionCatchingUpCannotHandleNewEvents(partition);
            return false;
        }

        return true;
    }

    async Task ResumeJobs()
    {
        var unfilteredJobs = await _jobsManager.GetAllJobs();
        var jobs = unfilteredJobs.Where(_ =>
                        _.Request is IObserverJobRequest &&
                        _.IsResumable).ToArray();
        foreach (var job in jobs)
        {
            await _jobsManager.Resume(job.Id);
        }
    }

    void HandleNewLastHandledEvent(EventSequenceNumber lastHandledEvent)
    {
        if (!lastHandledEvent.IsActualValue)
        {
            logger.LastHandledEventIsNotActualValue();
            return;
        }

        var newLastHandledEvent = State.LastHandledEventSequenceNumber == EventSequenceNumber.Unavailable ||
                                  State.LastHandledEventSequenceNumber < lastHandledEvent ? lastHandledEvent : State.LastHandledEventSequenceNumber;
        var nextEventSequenceNumber = State.NextEventSequenceNumber <= lastHandledEvent ? lastHandledEvent.Next() : State.NextEventSequenceNumber;
        State = State with
        {
            LastHandledEventSequenceNumber = newLastHandledEvent,
            NextEventSequenceNumber = nextEventSequenceNumber
        };
    }

    async Task StartRecoverJobForFailedPartition(FailedPartition failedPartition)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.TryingToRecoverFailedPartition(failedPartition.Partition);
        await RemoveReminder(failedPartition.Partition.ToString());
        await _jobsManager.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
            JobId.New(),
            new(
                _observerKey,
                _subscription,
                failedPartition.Partition,
                failedPartition.LastAttempt.SequenceNumber,
                State.EventTypes));
    }

    async Task RemoveReminder(Key partition)
    {
        var reminder = await this.GetReminder(partition.ToString());
        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder);
        }
    }

    async Task StartCatchupJobIfNeeded(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        if (failures.State.IsFailed(partition))
        {
            logger.PartitionToCatchUpIsFailing(partition);
            return;
        }
        if (!lastHandledEventSequenceNumber.IsActualValue)
        {
            logger.LastHandledEventIsNotActualValue();
            return;
        }
        var needCatchupResult = await NeedsCatchup(partition, lastHandledEventSequenceNumber);
        await needCatchupResult.Match(
            needCatchup => needCatchup
                ? StartCatchupJob(partition, lastHandledEventSequenceNumber)
                : Task.CompletedTask,
            error =>
            {
                switch (error)
                {
                    case GetSequenceNumberError.NotFound:
                        logger.LastHandledEventForPartitionUnavailable(partition);
                        return Task.CompletedTask;
                    default:
                        return PartitionFailed(partition, lastHandledEventSequenceNumber.Next(), ["Event Sequence storage error caused partition to try recover"], string.Empty);
                }
            });
    }

    async Task StartCatchupJob(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        var nextEventSequenceNumber = lastHandledEventSequenceNumber.Next();
        logger.StartingCatchUpForPartition(partition, nextEventSequenceNumber);
        State.CatchingUpPartitions.Add(partition);
        await _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            JobId.New(),
            new(
                _observerKey,
                _subscription,
                partition,
                nextEventSequenceNumber,
                State.EventTypes));
        await WriteStateAsync();
    }

    async Task<Result<bool, GetSequenceNumberError>> NeedsCatchup(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        var nextSequenceNumber = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(lastHandledEventSequenceNumber, State.EventTypes, partition);
        return nextSequenceNumber.Match<Result<bool, GetSequenceNumberError>>(
            number => number != lastHandledEventSequenceNumber,
            error => error);
    }

    bool IsJobForThisObserver(JobState jobState) =>
        ((ReplayObserverRequest)jobState.Request).ObserverKey == _observerKey;

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
