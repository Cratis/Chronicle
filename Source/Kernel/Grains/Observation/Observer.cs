// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserver"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Observers)]
public class Observer : StateMachine<ObserverState>, IObserver
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly ILogger<Observer> _logger;
    readonly IPersistentState<FailedPartitions> _failuresState;
    IStreamProvider _streamProvider = null!;
    ObserverId _observerId = Guid.Empty;
    ObserverKey _observerKey = ObserverKey.NotSet;
    ObserverSubscription _subscription;
    IJobsManager _jobsManager = null!;
    bool _stateWritingSuspended;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observer"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="failures"><see cref="IPersistentState{T}"/> for failed partitions.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Observer(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        [PersistentState(nameof(FailedPartition), WellKnownGrainStorageProviders.FailedPartitions)]
        IPersistentState<FailedPartitions> failures,
        ILogger<Observer> logger)
    {
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _failuresState = failures;
        _logger = logger;
        _subscription = ObserverSubscription.Unsubscribed;
    }

    FailedPartitions Failures => _failuresState.State;

    /// <inheritdoc/>
    public override Task OnActivation(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.MaxValue);

        _observerId = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);

        _streamProvider = this.GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        _jobsManager = GrainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_observerKey.MicroserviceId, _observerKey.TenantId));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SetNameAndType(ObserverName name, ObserverType type)
    {
        State.Name = name;
        State.Type = type;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<ObserverSubscription> GetSubscription() => Task.FromResult(_subscription);

    /// <inheritdoc/>
    public async Task Subscribe<TObserverSubscriber>(IEnumerable<EventType> eventTypes, object? subscriberArgs = null)
        where TObserverSubscriber : IObserverSubscriber
    {
        _logger.Subscribing(
            _observerId,
            _observerKey.MicroserviceId,
            _observerKey.TenantId,
            _observerKey.EventSequenceId);

        _subscription = new ObserverSubscription(
            _observerId,
            _observerKey,
            eventTypes,
            typeof(TObserverSubscriber),
            subscriberArgs);

        await TransitionTo<States.Routing>();
        await TryRecoverAllFailedPartitions();
    }

    /// <inheritdoc/>
    public override IImmutableList<IState<ObserverState>> CreateStates() => new IState<ObserverState>[]
    {
        new States.Disconnected(),
        new States.Routing(this, _eventSequenceStorageProvider()),
        new States.CatchUp(_observerKey, _jobsManager),
        new States.Replay(_observerKey, _jobsManager),
        new States.Indexing(),
        new States.Observing(
            this,
            _streamProvider,
            _observerKey.MicroserviceId,
            _observerKey.TenantId,
            _observerKey.EventSequenceId)
    }.ToImmutableList();

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        _subscription = ObserverSubscription.Unsubscribed;
        await TransitionTo<States.Disconnected>();
    }

    /// <inheritdoc/>
    public Task Replay() => TransitionTo<States.Replay>();

    /// <inheritdoc/>
    public Task ReplayPartition(Key partition) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task ReplayPartitionTo(Key partition, EventSequenceNumber sequenceNumber) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task PartitionFailed(Key partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace)
    {
        _logger.PartitionFailed(
            partition,
            sequenceNumber,
            _observerId,
            _observerKey.EventSequenceId,
            _observerKey.MicroserviceId,
            _observerKey.TenantId,
            _observerKey.SourceMicroserviceId ?? MicroserviceId.Unspecified,
            _observerKey.SourceTenantId ?? TenantId.NotSet);

        var failure = Failures.RegisterAttempt(partition, sequenceNumber, exceptionMessages, exceptionStackTrace);

        var time = TimeSpan.FromSeconds((failure.Attempts.Count() - 1) * 2).Add(TimeSpan.FromMilliseconds(100));
        RegisterTimer(
            async (_) => await TryRecoverFailedPartition(partition),
            null,
            time,
            TimeSpan.MaxValue);

        await _failuresState.WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task FailedPartitionRecovered(Key partition)
    {
        Failures.Remove(partition);
        return _failuresState.WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task TryRecoverFailedPartition(Key partition)
    {
        var failure = Failures.Get(partition);
        if (failure is null) return;
        var lastAttempt = failure.LastAttempt;
        if (lastAttempt is null) return;

        await _jobsManager.Start<RetryFailedPartitionJob, RetryFailedPartitionRequest>(
            JobId.New(),
            new(
                _observerId,
                _observerKey,
                _subscription,
                partition,
                lastAttempt.SequenceNumber,
                State.EventTypes));
    }

    /// <inheritdoc/>
    public async Task TryRecoverAllFailedPartitions()
    {
        foreach (var partition in Failures.Partitions)
        {
            await TryRecoverFailedPartition(partition.Partition);
        }
    }

    /// <inheritdoc/>
    public async Task Handle(Key partition, IEnumerable<AppendedEvent> events)
    {
        if (!_subscription.IsSubscribed || Failures.IsFailed(partition))
        {
            return;
        }

        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var tailEventSequenceNumber = State.NextEventSequenceNumber;

        events = events.Where(_ => _.Metadata.SequenceNumber >= State.NextEventSequenceNumber).ToArray();
        if (events.Any())
        {
            using (new WriteSuspension(this))
            {
                try
                {
                    var key = new ObserverSubscriberKey(
                        _observerKey.MicroserviceId,
                        _observerKey.TenantId,
                        _observerKey.EventSequenceId,
                        partition,
                        _observerKey.SourceMicroserviceId,
                        _observerKey.SourceTenantId);

                    var firstEvent = events.First();
                    var lastEvent = events.Last();

                    var subscriber = (GrainFactory.GetGrain(_subscription.SubscriberType, _observerId, key) as IObserverSubscriber)!;
                    tailEventSequenceNumber = firstEvent.Metadata.SequenceNumber;
                    var result = await subscriber.OnNext(events, new(_subscription.Arguments));
                    if (result.State == ObserverSubscriberState.Failed)
                    {
                        failed = true;
                        exceptionMessages = result.ExceptionMessages;
                        exceptionStackTrace = result.ExceptionStackTrace;
                        tailEventSequenceNumber = result.LastSuccessfulObservation == EventSequenceNumber.Unavailable ?
                            firstEvent.Metadata.SequenceNumber :
                            result.LastSuccessfulObservation;
                    }
                    else if (result.State == ObserverSubscriberState.Disconnected)
                    {
                        await Unsubscribe();
                        if (result.LastSuccessfulObservation == EventSequenceNumber.Unavailable)
                        {
                            return;
                        }
                    }

                    State.NextEventSequenceNumber = result.LastSuccessfulObservation.Next();
                    if (State.LastHandledEventSequenceNumber < result.LastSuccessfulObservation)
                    {
                        State.LastHandledEventSequenceNumber = result.LastSuccessfulObservation;
                    }
                }
                catch (Exception ex)
                {
                    failed = true;
                    exceptionMessages = ex.GetAllMessages().ToArray();
                    exceptionStackTrace = ex.StackTrace ?? string.Empty;
                }
            }

            if (failed)
            {
                await PartitionFailed(partition, tailEventSequenceNumber, exceptionMessages, exceptionStackTrace);
            }

            await WriteStateAsync();
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
    protected override async Task OnAfterEnteringState(IState<ObserverState> state)
    {
        if (state is States.BaseObserverState observerState)
        {
            State.RunningState = observerState.RunningState;
            await WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    protected override Task WriteStateAsync()
    {
        if (_stateWritingSuspended) return Task.CompletedTask;
        return base.WriteStateAsync();
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
