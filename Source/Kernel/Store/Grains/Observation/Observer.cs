// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.EventLogs;
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
    const string RecoverReminder = "partitioned-observer-failure-recovery";
    readonly ILogger<Observer> _logger;
    StreamSubscriptionHandle<AppendedEvent>? _subscription;
    IAsyncStream<AppendedEvent>? _stream;
    ObserverId _observerId = Guid.Empty;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IGrainReminder? _recoverReminder;
    string _connectionId = string.Empty;

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

        var streamProvider = GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        var microserviceAndTenant = new MicroserviceAndTenant(_microserviceId, _tenantId);
        _stream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, microserviceAndTenant);

        _recoverReminder = await GetReminder(RecoverReminder);
        foreach (var failedPartition in State.FailedPartitions)
        {
            if (_recoverReminder == default)
            {
                await HandleReminderRegistration(failedPartition.EventSourceId);
            }
        }

        if (!State.HasFailedPartitions && _recoverReminder != default)
        {
            await UnregisterReminder(_recoverReminder);
        }

        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task Rewind()
    {
        _logger.Rewinding(_microserviceId, _eventSequenceId, _tenantId);
        State.RunningState = ObserverRunningState.Rewinding;
        State.Offset = 0;
        await WriteStateAsync();
        await Unsubscribe();
        await Subscribe(State.EventTypes);
    }

    /// <inheritdoc/>
    public async Task Subscribe(IEnumerable<EventType> eventTypes)
    {
        _logger.Subscribing(_microserviceId, _eventSequenceId, _tenantId);
        _subscription?.UnsubscribeAsync();

        if (HasDefinitionChanged(eventTypes))
        {
            State.Offset = 0;
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

            await UnregisterReminder(reminder);

            // TODO: When an observer is recovering. We need to catch it up before we let register it as recovered.
            //       While it is catching up, we should ignore trying to recover it.
            // var streamProvider = GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
            // var microserviceAndTenant = new MicroserviceAndTenant(_microserviceId, _tenantId);
            // var eventLogStream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, microserviceAndTenant);
            // StreamSubscriptionHandle<AppendedEvent>? subscriptionId = null;
            // subscriptionId = await eventLogStream.SubscribeAsync(
            //     async (@event, _) =>
            //     {
            //         await HandleEventForPartitionedObserver(@event, _);
            //         if (!State.IsPartitionFailed(@event.Context.EventSourceId))
            //         {
            //             await WriteStateAsync();
            //             await subscriptionId!.UnsubscribeAsync();
            //         }
            //     },
            //     new EventLogSequenceNumberTokenWithFilter(State.SequenceNumber, State.EventTypes, _eventSourceId));
        }
    }

    /// <inheritdoc/>
    public Task TryResumePartition(EventSourceId eventSourceId)
    {
        return Task.CompletedTask;
    }

    bool HasDefinitionChanged(IEnumerable<EventType> eventTypes) => !State.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(eventTypes.OrderBy(_ => _.Id.Value));

    async Task HandleEventForPartitionedObserver(AppendedEvent @event, StreamSequenceToken token)
    {
        try
        {
            if (State.IsPartitionFailed(@event.Context.EventSourceId) || string.IsNullOrEmpty(_connectionId))
            {
                return;
            }

            var partitionedObserver = GetPartitionedObserverFor(@event.Context.EventSourceId);

            await partitionedObserver.SetConnectionId(_connectionId);
            await partitionedObserver.OnNext(@event, State.EventTypes);

            State.Offset = @event.Metadata.SequenceNumber + 1;
            State.LastHandled = @event.Metadata.SequenceNumber + 1;
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
            await HandleReminderRegistration(@event.Context.EventSourceId);
        }
    }

    IPartitionedObserver GetPartitionedObserverFor(EventSourceId eventSourceId)
    {
        var key = new PartitionedObserverKey(_microserviceId, _tenantId, _eventSequenceId, eventSourceId);
        return GrainFactory.GetGrain<IPartitionedObserver>(_observerId, keyExtension: key);
    }

    async Task HandleReminderRegistration(EventSourceId eventSourceId)
    {
        // TODO: Reminder needs to be for the observer and we check all failed partitions.
        // if (State.IsPartitionFailed(eventSourceId))
        // {
        //     var failedPartition = State.GetFailedPartition(eventSourceId);
        //     if (failedPartition.Attempts <= 10)
        //     {
        //         _recoverReminder = await RegisterOrUpdateReminder(
        //             RecoverReminder,
        //             TimeSpan.FromSeconds(60),
        //             TimeSpan.FromSeconds(60) * failedPartition.Attempts);
        //     }
        //     else
        //     {
        //         var reminder = await GetReminder(RecoverReminder);
        //         if (reminder != null)
        //         {
        //             await UnregisterReminder(reminder);
        //         }
        //     }
        // }
        Console.WriteLine(eventSourceId);
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
