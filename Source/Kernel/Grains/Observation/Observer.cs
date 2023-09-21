// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserver"/>.
/// </summary>
[StorageProvider(ProviderName = ObserverState.StorageProvider)]
public class Observer : StateMachine<ObserverState>, IObserver
{
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly ILogger<Observer> _logger;
    IStreamProvider _streamProvider = null!;
    ObserverId _observerId = Guid.Empty;
    ObserverKey _observerKey = ObserverKey.NotSet;
    ObserverSubscription _subscription;
    bool _stateWritingSuspended;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observer"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Observer(
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        ILogger<Observer> logger)
    {
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _logger = logger;
        _subscription = ObserverSubscription.Unsubscribed;
    }

    /// <inheritdoc/>
    public override Task OnActivation(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.MaxValue);

        _observerId = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);

        _streamProvider = this.GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);

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

        await TransitionTo<States.Subscribing>();
    }

    /// <inheritdoc/>
    public override IImmutableList<IState<ObserverState>> CreateStates() => new IState<ObserverState>[]
    {
        new States.Disconnected(),
        new States.Subscribing(this, _eventSequenceStorageProvider()),
        new States.CatchUp(),
        new States.Replay(),
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
    public Task ReplayPartition(EventSourceId partition) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task ReplayPartitionTo(EventSourceId partition, EventSequenceNumber sequenceNumber) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task PartitionFailed(EventSourceId partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace)
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

        State.AddFailedPartition(new(partition, sequenceNumber, exceptionMessages, exceptionStackTrace));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task TryResumePartition(EventSourceId partition) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task Handle(EventSourceId eventSourceId, IEnumerable<AppendedEvent> events)
    {
        if (!_subscription.IsSubscribed || State.IsPartitionFailed(eventSourceId))
        {
            return;
        }

        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var tailEventSequenceNumber = State.NextEventSequenceNumber;

        using (new WriteSuspension(this))
        {
            try
            {
                events = events.Where(_ => _.Metadata.SequenceNumber >= State.NextEventSequenceNumber).ToArray();
                if (events.Any())
                {
                    var key = new ObserverSubscriberKey(
                        _observerKey.MicroserviceId,
                        _observerKey.TenantId,
                        _observerKey.EventSequenceId,
                        eventSourceId,
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
                        tailEventSequenceNumber = result.LastSuccessfulObservation;
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
                    if (State.LastHandled < result.LastSuccessfulObservation)
                    {
                        State.LastHandled = result.LastSuccessfulObservation;
                    }
                }
            }
            catch (Exception ex)
            {
                failed = true;
                exceptionMessages = ex.GetAllMessages().ToArray();
                exceptionStackTrace = ex.StackTrace ?? string.Empty;
            }
            if (failed)
            {
                await PartitionFailed(eventSourceId, tailEventSequenceNumber, exceptionMessages, exceptionStackTrace);
            }
        }
        await WriteStateAsync();
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
