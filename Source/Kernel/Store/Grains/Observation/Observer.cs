// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.EventSequences;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserver"/>.
/// </summary>
[StorageProvider(ProviderName = ObserverState.StorageProvider)]
public class Observer : Grain<ObserverState>, IObserver, IRemindable
{
    /// <summary>
    /// The name of the recover reminder.
    /// </summary>
    public const string RecoverReminder = "observer-failure-recovery";
    readonly ILogger<Observer> _logger;
    StreamSubscriptionHandle<AppendedEvent>? _subscription;
    IAsyncStream<AppendedEvent>? _stream;
    ObserverId _observerId = Guid.Empty;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IGrainReminder? _recoverReminder;
    string _connectionId = string.Empty;
    IEventSequence? _eventSequence;

    bool IsConnected => !string.IsNullOrEmpty(_connectionId);

    /// <summary>
    /// Initializes a new instance of the <see cref="Observer"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public Observer(ILogger<Observer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverKey.Parse(keyAsString);
        _eventSequenceId = key.EventSequenceId;
        _microserviceId = key.MicroserviceId;
        _tenantId = key.TenantId;

        _eventSequence = GrainFactory.GetGrain<IEventSequence>(_eventSequenceId, new MicroserviceAndTenant(_microserviceId, _tenantId));

        var streamProvider = GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        var microserviceAndTenant = new MicroserviceAndTenant(_microserviceId, _tenantId);
        _stream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, microserviceAndTenant);

        _recoverReminder = await GetReminder(RecoverReminder);
        await HandleReminderRegistration();
        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task Rewind()
    {
        _logger.Rewinding(_microserviceId, _eventSequenceId, _tenantId);
        State.RunningState = ObserverRunningState.Rewinding;
        State.Offset = EventSequenceNumber.First;
        await WriteStateAsync();
        await Unsubscribe();
        await Subscribe(State.EventTypes);
    }

    /// <inheritdoc/>
    public async Task Subscribe(IEnumerable<EventType> eventTypes)
    {
        _logger.Subscribing(_microserviceId, _eventSequenceId, _tenantId);
        State.RunningState = ObserverRunningState.Subscribing;
        _subscription?.UnsubscribeAsync();

        if (HasDefinitionChanged(eventTypes))
        {
            State.RunningState = ObserverRunningState.Rewinding;
            State.Offset = EventSequenceNumber.First;
        }

        var nextSequenceNumber = await _eventSequence!.GetNextSequenceNumber();
        if (State.Offset < nextSequenceNumber)
        {
            State.RunningState = ObserverRunningState.CatchingUp;
        }

        if (State.Offset == nextSequenceNumber)
        {
            State.RunningState = ObserverRunningState.Active;
        }

        State.EventTypes = eventTypes;
        await WriteStateAsync();

        _subscription = await _stream!.SubscribeAsync(
            HandleEventForPartitionedObserver,
            new EventLogSequenceNumberTokenWithFilter(State.Offset, eventTypes));
    }

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        _logger.Unsubscribing(_microserviceId, _eventSequenceId, _tenantId);
        if (_subscription is not null)
        {
            await _subscription.UnsubscribeAsync();
            _subscription = null;
        }
    }

    /// <inheritdoc/>
    public void Disconnected()
    {
        _connectionId = string.Empty;
        Unsubscribe().Wait();
    }

    /// <inheritdoc/>
    public Task SetConnectionId(string connectionId)
    {
        _connectionId = connectionId;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName == RecoverReminder)
        {
            var reminder = await GetReminder(RecoverReminder);
            if (reminder == default)
            {
                return;
            }

            if (!State.HasFailedPartitions)
            {
                await UnregisterReminder(reminder);
                return;
            }

            foreach (var failedPartition in State.FailedPartitions)
            {
                await TryResumePartition(failedPartition.EventSourceId);
            }
        }
    }

    /// <inheritdoc/>
    public async Task TryResumePartition(EventSourceId eventSourceId)
    {
        if (!IsConnected || !State.IsPartitionFailed(eventSourceId))
        {
            return;
        }

        var failedPartition = State.GetFailedPartition(eventSourceId);
        if (State.IsRecoveringPartition(failedPartition.EventSourceId))
        {
            return;
        }

        StreamSubscriptionHandle<AppendedEvent>? subscriptionId = null;
        State.StartRecoveringPartition(eventSourceId);
        subscriptionId = await _stream.SubscribeAsync(
            async (@event, token) => await HandleEventForRecoveringPartitionedObserver(@event, token, subscriptionId),
            new EventLogSequenceNumberTokenWithFilter(failedPartition.SequenceNumber, State.EventTypes, eventSourceId));
    }

    bool HasDefinitionChanged(IEnumerable<EventType> eventTypes) => !State.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(eventTypes.OrderBy(_ => _.Id.Value));

    async Task HandleEventForRecoveringPartitionedObserver(AppendedEvent @event, StreamSequenceToken token, StreamSubscriptionHandle<AppendedEvent>? subscriptionId)
    {
        await HandleEventForPartitionedObserver(@event, token);
        if (State.IsPartitionFailed(@event.Context.EventSourceId))
        {
            await subscriptionId!.UnsubscribeAsync();
        }
        else
        {
            var partitionRecovery = State.GetPartitionRecovery(@event.Context.EventSourceId);
            partitionRecovery.SequenceNumber++;
            await WriteStateAsync();

            var nextSequenceNumber = await _eventSequence!.GetNextSequenceNumber();
            if (partitionRecovery.SequenceNumber == nextSequenceNumber)
            {
                State.PartitionRecovered(@event.Context.EventSourceId);
            }
        }
    }

    async Task HandleEventForPartitionedObserver(AppendedEvent @event, StreamSequenceToken token)
    {
        try
        {
            if (State.IsPartitionFailed(@event.Context.EventSourceId) ||
                State.IsRecoveringPartition(@event.Context.EventSourceId) ||
                !IsConnected)
            {
                return;
            }

            var partitionedObserver = GetPartitionedObserverFor(@event.Context.EventSourceId);

            await partitionedObserver.SetConnectionId(_connectionId);
            await partitionedObserver.OnNext(@event, State.EventTypes);

            State.Offset = @event.Metadata.SequenceNumber + 1;
            State.LastHandled = @event.Metadata.SequenceNumber + 1;

            var nextSequenceNumber = await _eventSequence!.GetNextSequenceNumber();
            if (State.Offset == nextSequenceNumber)
            {
                State.RunningState = ObserverRunningState.Active;
            }
            await WriteStateAsync();
        }
        catch (Exception ex)
        {
            State.FailPartition(
                @event.Context.EventSourceId,
                @event.Metadata.SequenceNumber,
                GetMessagesFromException(ex),
                ex.StackTrace ?? string.Empty);

            await WriteStateAsync();
            await HandleReminderRegistration();
        }
    }

    IPartitionedObserver GetPartitionedObserverFor(EventSourceId eventSourceId)
    {
        var key = new PartitionedObserverKey(_microserviceId, _tenantId, _eventSequenceId, eventSourceId);
        return GrainFactory.GetGrain<IPartitionedObserver>(_observerId, keyExtension: key);
    }

    async Task HandleReminderRegistration()
    {
        if (!State.HasFailedPartitions && _recoverReminder != default)
        {
            await UnregisterReminder(_recoverReminder);
        }
        if (State.HasFailedPartitions && _recoverReminder == default)
        {
            var anyPartitionedToRetry = State.FailedPartitions.Any(_ => _.Attempts <= 10);
            if (anyPartitionedToRetry)
            {
                _recoverReminder = await RegisterOrUpdateReminder(
                    RecoverReminder,
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(60));
            }
            else
            {
                var reminder = await GetReminder(RecoverReminder);
                if (reminder is not null)
                {
                    await UnregisterReminder(reminder);
                }
                _recoverReminder = null;
            }
        }

        await Task.CompletedTask;
    }

    string[] GetMessagesFromException(Exception ex)
    {
        var messages = new List<string>
                {
                    ex.Message
                };
        while (ex.InnerException != null)
        {
            messages.Insert(0, ex.InnerException.Message);
            ex = ex.InnerException;
        }
        return messages.ToArray();
    }
}
