// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <inheritdoc cref="IRecoverFailedPartition" />
[StorageProvider(ProviderName = RecoverFailedPartitionState.StorageProvider)]
public class RecoverFailedPartition : Grain<RecoverFailedPartitionState>, IRecoverFailedPartition
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly ILogger<RecoverFailedPartition> _logger;
    PartitionedObserverKey? _key;
    ObserverKey? _observerKey;
    ObserverSubscriberKey? _subscriberKey;
    ObserverSubscription? _subscriberSubscription;
    IDisposable? _timer;

    /// <summary>
    /// Gets the <see cref="IEventSequenceStorage"/> in the correct context.
    /// </summary>
    protected IEventSequenceStorage EventSequenceStorageProvider
    {
        get
        {
            var tenantId = _key!.TenantId;
            var microserviceId = _key.MicroserviceId;

            // TODO: This is a temporary work-around till we fix #264 & #265
            _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
            return _eventSequenceStorageProvider();
        }
    }

    /// <summary>
    /// Instantiates an instance of <see cref="RecoverFailedPartition"/>.
    /// </summary>
    /// <param name="executionContextManager">The Execution Context Manager.</param>
    /// <param name="eventSequenceStorageProvider">The Event Sequence Storage Provider.</param>
    /// <param name="logger">A logger.</param>
    public RecoverFailedPartition(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        ILogger<RecoverFailedPartition> logger)
    {
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        State.ObserverId = this.GetPrimaryKey(out var keyAsString);
        _key = PartitionedObserverKey.Parse(keyAsString);
        State.Id = _key.ToString();
        State.Partition = _key.EventSourceId;
        State.EventSequenceId = _key.EventSequenceId;
        _observerKey = string.IsNullOrWhiteSpace(State.ObserverKey) ? null : ObserverKey.Parse(State.ObserverKey);
        _subscriberKey = string.IsNullOrWhiteSpace(State.SubscriberKey) ? null : ObserverSubscriberKey.Parse(State.SubscriberKey);
        _logger.Activating(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId);
        await SetSubscriberSubscription();
        ScheduleNextTimer();
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.Deactivating(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId);
        await WriteStateAsync();
        if (_timer is not null)
        {
            _timer.Dispose();
            _timer = null;
        }
    }

    /// <inheritdoc/>
    public async Task Recover(ObserverKey observerKey, ObserverName observerName, EventSequenceNumber fromEvent, IEnumerable<EventType> eventTypes, IEnumerable<string> messages, string stackTrace)
    {
        _observerKey = observerKey;
        _subscriberKey = ObserverSubscriberKey.FromObserverKey(observerKey, _key!.EventSourceId);
        State.InitializeError(_observerKey, observerName, _subscriberKey, fromEvent, eventTypes, messages, stackTrace);
        _logger.RecoveryRequested(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId, fromEvent);
        await SetSubscriberSubscription();
        await WriteStateAsync();
        ScheduleNextTimer();
    }

    /// <inheritdoc/>
    public async Task Catchup(EventSequenceNumber fromEvent)
    {
        _logger.CatchupRequested(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId, fromEvent);
        State.Catchup(fromEvent);
        await SetSubscriberSubscription();
        await WriteStateAsync();
        ScheduleNextTimer(isCatchup: true);
    }

    /// <inheritdoc/>
    public async Task Reset()
    {
        _logger.ResetRequested(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId);
        State.Reset();
        _timer?.Dispose();
        await WriteStateAsync();
    }

    /// <summary>
    /// Handles the processing of an appended event.
    /// </summary>
    /// <param name="event">The event to be processed.</param>
    /// <returns>Returns a task indicating if the processing succeeded or not.</returns>
    public async Task<bool> Handle(AppendedEvent @event)
    {
        var eventSequenceNumber = @event.Metadata.SequenceNumber;
        _logger.ReceivedEventForProcessing(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId, eventSequenceNumber);
        var failed = false;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        try
        {
            var next = State.NextSequenceNumberToProcess;

            if (@event.Metadata.SequenceNumber < next)
            {
                _logger.EventForProcessingIgnored(State.ObserverId, _key.MicroserviceId, _key.TenantId, _key.EventSequenceId, _key.EventSourceId, eventSequenceNumber, State.NextSequenceNumberToProcess);
                return true;
            }

            if (_subscriberSubscription is not null)
            {
                var subscriber = (GrainFactory.GetGrain(_subscriberSubscription.SubscriberType, State.ObserverId, _subscriberKey!) as IObserverSubscriber)!;
                var result = await subscriber.OnNext(@event, new ObserverSubscriberContext(_subscriberSubscription.Arguments));
                switch (result.State)
                {
                    case ObserverSubscriberState.Failed:
                        failed = true;
                        exceptionMessages = result.ExceptionMessages;
                        exceptionStackTrace = result.ExceptionStackTrace;
                        break;
                    case ObserverSubscriberState.Disconnected:
                        failed = true;
                        exceptionMessages = new[] { "Subscriber disconnected" };
                        exceptionStackTrace = string.Empty;
                        break;
                }
            }
            else
            {
                failed = true;
                _logger.MissingSubscriberSubscription(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId, eventSequenceNumber);
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
            State.UpdateWithLatestError(@event.Metadata.SequenceNumber, exceptionMessages, exceptionStackTrace, DateTimeOffset.UtcNow);
            _logger.SubscriberEventProcessingFailed(eventSequenceNumber, State.ObserverId, _key.MicroserviceId, _key.TenantId, _key.EventSequenceId, _key.EventSourceId, State.NumberOfAttemptsOnCurrentError, State.NumberOfAttemptsOnSinceInitialized);
        }
        else
        {
            State.UpdateWithLatestSuccess(@event);
            _logger.SubscriberEventProcessed(eventSequenceNumber, State.ObserverId, _key.MicroserviceId, _key.TenantId, _key.EventSequenceId, _key.EventSourceId, State.NextSequenceNumberToProcess);
        }
        await WriteStateAsync();
        return failed;
    }

    async Task PerformRecovery(object state)
    {
        _logger.RecoveryProcessingTriggered(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId, State.NextSequenceNumberToProcess);
        var provider = EventSequenceStorageProvider;
        _timer?.Dispose(); // we don't want to run this again until we are done
        using var cursor = await provider.GetFromSequenceNumber(_key!.EventSequenceId!, State.NextSequenceNumberToProcess, _key.EventSourceId, eventTypes: State.EventTypes);
        var completed = true;

        var eventSequenceNumber = EventSequenceNumber.Unavailable;
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                eventSequenceNumber = @event.Metadata.SequenceNumber;
                if (!await Handle(@event)) continue;
                completed = false;
                break;
            }
        }

        if (completed)
        {
            _logger.ProcessingCompleted(State.ObserverId, _key.MicroserviceId, _key.TenantId, _key.EventSequenceId, _key.EventSourceId, eventSequenceNumber);
            await GetSupervisor(State.ObserverId, _observerKey!).NotifyFailedPartitionRecoveryComplete(_key.EventSourceId, eventSequenceNumber);
        }
        else
        {
            _logger.ProcessingIncomplete(State.ObserverId, _key.MicroserviceId, _key.TenantId, _key.EventSequenceId, _key.EventSourceId, eventSequenceNumber);
            ScheduleNextTimer();
        }
    }

    void ScheduleNextTimer(bool isCatchup = false)
    {
        if (State.HasBeenInitialized())
        {
            var nextAttempt = isCatchup ? TimeSpan.Zero : State.GetNextAttemptSchedule();
            _logger.ProcessingScheduled(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId, nextAttempt, State.NextSequenceNumberToProcess);
            _timer = RegisterTimer(PerformRecovery, null, nextAttempt, TimeSpan.FromHours(1));
        }
        _logger.ProcessingScheduleIgnored(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId);
    }

    async Task SetSubscriberSubscription()
    {
        if (_observerKey is not null && State.ObserverId != ObserverId.Unspecified)
        {
            _subscriberSubscription = await GetSupervisor(State.ObserverId, _observerKey).GetCurrentSubscription();
            return;
        }
        _logger.UnableToGetSubscriberSubscription(State.ObserverId, _key!.MicroserviceId, _key!.TenantId, _key!.EventSequenceId, _key!.EventSourceId);
    }

    IObserverSupervisor GetSupervisor(ObserverId observerId, ObserverKey key) => GrainFactory.GetGrain<IObserverSupervisor>(observerId, key);
}
