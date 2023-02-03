// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents a base class for all observers containing common methods and functionality.
/// </summary>
public abstract class Observer : Grain
{
    readonly ILogger<Observer> _logger;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProviderProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly IPersistentState<ObserverState> _observerState;
    IObserverSupervisor? _supervisor;

    /// <summary>
    /// Gets or sets the subscriber type.
    /// </summary>
    protected Type SubscriberType { get; set; } = typeof(IObserverSubscriber);

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
    /// Gets the <see cref="IObserverSupervisor"/>.
    /// </summary>
    protected IObserverSupervisor Supervisor => _supervisor ??= this switch
    {
        IObserverSupervisor supervisor => supervisor,
        _ => GrainFactory.GetGrain<IObserverSupervisor>(ObserverId, new ObserverKey(MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId))
    };

    /// <summary>
    /// Gets the <see cref="IEventSequenceStorageProvider"/> in the correct context.
    /// </summary>
    protected IEventSequenceStorageProvider EventSequenceStorageProvider
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
    /// Initializes a new instance of the <see cref="Observer"/> class.
    /// </summary>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    /// <param name="eventSequenceStorageProviderProvider"><see creF="IEventSequenceStorageProvider"/> for working with the underlying event sequence.</param>
    /// <param name="observerState"><see cref="IPersistentState{T}"/> for the <see cref="Observation.ObserverState"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    protected Observer(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProviderProvider,
        IPersistentState<ObserverState> observerState,
        ILogger<Observer> logger)
    {
        _eventSequenceStorageProviderProvider = eventSequenceStorageProviderProvider;
        _executionContextManager = executionContextManager;
        _observerState = observerState;
        _logger = logger;
    }

    /// <summary>
    /// Read the observer state
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected Task ReadStateAsync() => _observerState.ReadStateAsync();

    /// <summary>
    /// Write the observer state
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected Task WriteStateAsync() => _observerState.WriteStateAsync();

#pragma warning disable IDE0060 // allow unused parameter
    /// <summary>
    /// Represents a filter for event types.
    /// </summary>
    /// <param name="streamIdentity">The stream identity.</param>
    /// <param name="filterData">Data associated with the filter.</param>
    /// <param name="item">Item to filter.</param>
    /// <returns>True if its to be included, false if not.</returns>
    public static bool EventTypesFilter(IStreamIdentity streamIdentity, object filterData, object item)
    {
        var appendedEvent = (item as AppendedEvent)!;
        var eventTypes = (filterData as EventType[])!;
        if (eventTypes.Length == 0)
        {
            return true;
        }
        return eventTypes.Any(_ => _.Id.Equals(appendedEvent.Metadata.Type.Id));
    }

    /// <summary>
    /// Represents a filter for event types.
    /// </summary>
    /// <param name="streamIdentity">The stream identity.</param>
    /// <param name="filterData">Data associated with the filter.</param>
    /// <param name="item">Item to filter.</param>
    /// <returns>True if its to be included, false if not.</returns>
    public static bool EventTypesAndEventSourceIdFilter(IStreamIdentity streamIdentity, object filterData, object item)
    {
        var appendedEvent = (item as AppendedEvent)!;
        var eventTypesAndEventSourceId = (filterData as EventTypesAndEventSourceId)!;

        var shouldIncludeEventType =
            eventTypesAndEventSourceId.EventTypes.Any(_ => _.Id.Equals(appendedEvent.Metadata.Type.Id)) ||
            eventTypesAndEventSourceId.EventTypes.Length == 0;

        return
            appendedEvent.Context.EventSourceId == eventTypesAndEventSourceId.EventSourceId &&
            shouldIncludeEventType;
    }
#pragma warning restore IDE0060

    /// <summary>
    /// Handle an <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/> to handle.</param>
    /// <param name="setLastHandled">Whether or not to set last handled.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(AppendedEvent @event, bool setLastHandled = false)
    {
        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        try
        {
            if (State.IsDisconnected)
            {
                return;
            }

            var next = State.NextEventSequenceNumber;
            if (State.IsPartitionFailed(@event.Context.EventSourceId))
            {
                next = State.GetFailedPartition(@event.Context.EventSourceId).SequenceNumber;
            }

            if (@event.Metadata.SequenceNumber < next)
            {
                return;
            }

            var key = new ObserverSubscriberKey(
                MicroserviceId,
                TenantId,
                EventSequenceId,
                @event.Context.EventSourceId,
                SourceMicroserviceId,
                SourceTenantId);

            if (SubscriberType is not null)
            {
                var subscriber = (GrainFactory.GetGrain(SubscriberType, ObserverId, key) as IObserverSubscriber)!;
                var result = await subscriber.OnNext(@event);
                if (result.State == ObserverSubscriberState.Error)
                {
                    failed = true;
                    exceptionMessages = result.ExceptionMessages;
                    exceptionStackTrace = result.ExceptionStackTrace;
                }
                else if (result.State == ObserverSubscriberState.Disconnected)
                {
                    return;
                }
                State.NextEventSequenceNumber = @event.Metadata.SequenceNumber + 1;
                if (setLastHandled)
                {
                    State.LastHandled = @event.Metadata.SequenceNumber;
                }

                await WriteStateAsync();
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
            _logger.PartitionFailed(
                @event.Context.EventSourceId,
                @event.Context.SequenceNumber,
                ObserverId,
                EventSequenceId,
                MicroserviceId,
                TenantId,
                SourceMicroserviceId ?? MicroserviceId.Unspecified,
                SourceTenantId ?? TenantId.NotSet);

            await Supervisor.PartitionFailed(@event, exceptionMessages, exceptionStackTrace);
        }
    }
}
