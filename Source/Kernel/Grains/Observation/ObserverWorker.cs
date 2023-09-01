// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents a base class for all observers containing common methods and functionality.
/// </summary>
public abstract class ObserverWorker : Grain
{
    readonly ILogger<ObserverWorker> _logger;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProviderProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly IPersistentState<ObserverState> _observerState;
    ObserverSubscription _currentSubscription = ObserverSubscription.Unsubscribed;
    IObserverSupervisor? _supervisor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverWorker"/> class.
    /// </summary>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    /// <param name="eventSequenceStorageProviderProvider"><see creF="IEventSequenceStorage"/> for working with the underlying event sequence.</param>
    /// <param name="observerState"><see cref="IPersistentState{T}"/> for the <see cref="ObserverState"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    protected ObserverWorker(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProviderProvider,
        IPersistentState<ObserverState> observerState,
        ILogger<ObserverWorker> logger)
    {
        _eventSequenceStorageProviderProvider = eventSequenceStorageProviderProvider;
        _executionContextManager = executionContextManager;
        _observerState = observerState;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the subscriber type.
    /// </summary>
    protected ObserverSubscription CurrentSubscription
    {
        get => _currentSubscription;
        set
        {
            _currentSubscription = value;
            State.CurrentSubscriptionType = _currentSubscription.SubscriberType.AssemblyQualifiedName;
            State.CurrentSubscriptionArguments = _currentSubscription.Arguments;
        }
    }

    /// <summary>
    /// Gets the <see cref="ObserverState"/>.
    /// </summary>
    protected ObserverState State => _observerState.State;

    /// <summary>
    /// Gets the <see cref="MicroserviceId"/>.
    /// </summary>
    protected abstract MicroserviceId MicroserviceId { get; }

    /// <summary>
    /// Gets the <see cref="TenantId"/>.
    /// </summary>
    protected abstract TenantId TenantId { get; }

    /// <summary>
    /// Gets the <see cref="EventSequenceId"/>.
    /// </summary>
    protected abstract EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets the source <see cref="MicroserviceId"/>.
    /// </summary>
    protected abstract MicroserviceId? SourceMicroserviceId { get; }

    /// <summary>
    /// Gets the source <see cref="TenantId"/>.
    /// </summary>
    protected abstract TenantId? SourceTenantId { get; }

    /// <summary>
    /// Gets the <see cref="ObserverId"/>.
    /// </summary>
    protected ObserverId ObserverId => State.ObserverId;

    /// <summary>
    /// Gets a value indicating whether or not the observer is active.
    /// </summary>
    protected bool IsActive => !State.IsDisconnected && CurrentSubscription.IsSubscribed;

    /// <summary>
    /// Gets a value indicating whether or not this worker is a supervisor.
    /// </summary>
    protected bool IsSupervisor => this is IObserverSupervisor;

    /// <summary>
    /// Gets the <see cref="IObserverSupervisor"/>.
    /// </summary>
    protected IObserverSupervisor Supervisor => _supervisor ??= this switch
    {
        IObserverSupervisor supervisor => supervisor,
        _ => GrainFactory.GetGrain<IObserverSupervisor>(ObserverId, new ObserverKey(MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId))
    };

    /// <summary>
    /// Gets the <see cref="IEventSequenceStorage"/> in the correct context.
    /// </summary>
    protected IEventSequenceStorage EventSequenceStorageProvider
    {
        get
        {
            var tenantId = SourceTenantId ?? TenantId;
            var microserviceId = SourceMicroserviceId ?? MicroserviceId;

            // TODO: This is a temporary work-around till we fix #264 & #265
            _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
            return _eventSequenceStorageProviderProvider();
        }
    }

    /// <summary>
    /// Notify that the partition has failed.
    /// </summary>
    /// <param name="partition">The partition that failed.</param>
    /// <param name="sequenceNumber">The sequence number of the failure.</param>
    /// <param name="exceptionMessages">All exception messages.</param>
    /// <param name="exceptionStackTrace">The exception stacktrace.</param>
    /// <returns>Awaitable task.</returns>
    public virtual Task PartitionFailed(EventSourceId partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace)
    {
        State.AddFailedPartition(new(partition, sequenceNumber, exceptionMessages, exceptionStackTrace));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handle an <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/> to handle.</param>
    /// <param name="ignoreSequenceNumber">Optionally set whether or not to ignore sequence number and force a handle. If set to true, it will not update sequence number either. Defaults to false.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(AppendedEvent @event, bool ignoreSequenceNumber = false)
    {
        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        try
        {
            if (!IsActive)
            {
                return;
            }

            if (@event.Metadata.SequenceNumber >= State.NextEventSequenceNumber || ignoreSequenceNumber)
            {
                if (!State.IsPartitionFailed(@event.Context.EventSourceId))
                {
                    var result = await OnNext(@event);
                    if (result.State == ObserverSubscriberState.Failed)
                    {
                        failed = true;
                        exceptionMessages = result.ExceptionMessages;
                        exceptionStackTrace = result.ExceptionStackTrace;
                    }
                    else if (result.State == ObserverSubscriberState.Disconnected)
                    {
                        return;
                    }
                }

                if (!ignoreSequenceNumber)
                {
                    State.NextEventSequenceNumber = @event.Metadata.SequenceNumber.Next();
                    if (State.LastHandled < @event.Metadata.SequenceNumber)
                    {
                        State.LastHandled = @event.Metadata.SequenceNumber;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            failed = true;
            exceptionMessages = ex.GetAllMessages().ToArray();
            exceptionStackTrace = ex.StackTrace ?? string.Empty;
        }

        await WriteStateAsync();

        if (failed)
        {
            _logger.PartitionFailed(
                @event.Context.EventSourceId,
                @event.Context.SequenceNumber,
                ObserverId,
                EventSequenceId,
                MicroserviceId,
                TenantId,
                SourceMicroserviceId ?? MicroserviceId.Unspecified,
                SourceTenantId ?? TenantId.NotSet);

            await PartitionFailed(@event.Context.EventSourceId, @event.Context.SequenceNumber, exceptionMessages, exceptionStackTrace);

            if (!IsSupervisor)
            {
                await ReadStateAsync();
            }
        }
    }

    /// <summary>
    /// Perform OnNext on the subscriber.
    /// </summary>
    /// <param name="event">Appended event.</param>
    /// <returns><see cref="ObserverSubscriberResult"/>.</returns>
    protected Task<ObserverSubscriberResult> OnNext(AppendedEvent @event)
    {
        var key = new ObserverSubscriberKey(
            MicroserviceId,
            TenantId,
            EventSequenceId,
            @event.Context.EventSourceId,
            SourceMicroserviceId,
            SourceTenantId);

        var subscriber = (GrainFactory.GetGrain(CurrentSubscription.SubscriberType, ObserverId, key) as IObserverSubscriber)!;
        return subscriber.OnNext(@event, new(CurrentSubscription.Arguments));
    }

    /// <summary>
    /// Read the observer state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task ReadStateAsync()
    {
        var subscriptionType = State.CurrentSubscriptionType;
        var subscriptionArguments = State.CurrentSubscriptionArguments;
        await _observerState.ReadStateAsync();
        if (string.IsNullOrEmpty(subscriptionType))
        {
            CurrentSubscription = ObserverSubscription.Unsubscribed;
        }
        else
        {
            _logger.SubscribedObserver(ObserverId, EventSequenceId, MicroserviceId, TenantId, State.CurrentSubscriptionType);
            CurrentSubscription = new(
                ObserverId,
                new(MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId),
                State.EventTypes,
                Type.GetType(subscriptionType)!,
                subscriptionArguments!);
        }
    }

    /// <summary>
    /// Write the observer state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected Task WriteStateAsync() => _observerState.WriteStateAsync();
}
