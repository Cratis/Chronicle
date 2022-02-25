// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.EventLogs;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IPartitionedObserver"/>.
/// </summary>
[StorageProvider(ProviderName = FailedObserverState.StorageProvider)]
public class PartitionedObserver : Grain<FailedObserverState>, IPartitionedObserver, IRemindable
{
    const string RecoverReminder = "partitioned-observer-failure-recovery";
    ObserverId _observerId = ObserverId.Unspecified;
    IAsyncStream<AppendedEvent>? _stream;
    IGrainReminder? _recoverReminder;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    string _connectionId = string.Empty;

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var extension);
        var key = PartitionedObserverKey.Parse(extension);
        _microserviceId = key.MicroserviceId;
        _tenantId = key.TenantId;
        _eventSequenceId = key.EventSequenceId;
        _eventSourceId = key.EventSourceId;

        _recoverReminder = await GetReminder(RecoverReminder);
        if (State.IsFailed)
        {
            if (_recoverReminder == default)
            {
                await HandleReminderRegistration();
            }
        }
        else if (_recoverReminder != default)
        {
            await UnregisterReminder(_recoverReminder);
        }

        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task OnNext(AppendedEvent @event, IEnumerable<EventType> eventTypes)
    {
        if (State.IsFailed)
        {
            return;
        }

        await HandleEvent(@event, eventTypes);
    }

    /// <inheritdoc/>
    public Task SetConnectionId(string connectionId)
    {
        if (_connectionId != connectionId || _stream == default)
        {
            var streamProvider = GetStreamProvider("observer-handlers");
            _stream = streamProvider.GetStream<AppendedEvent>(_observerId, connectionId);
        }
        _connectionId = connectionId!;

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

            var streamProvider = GetStreamProvider(EventSequence.StreamProvider);

            var eventLogStream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, _tenantId.ToString());
            StreamSubscriptionHandle<AppendedEvent>? subscriptionId = null;

            subscriptionId = await eventLogStream.SubscribeAsync(
                async (@event, _) =>
                {
                    await HandleEvent(@event, State.EventTypes);

                    if (!State.IsFailed)
                    {
                        State.IsFailed = false;
                        await WriteStateAsync();
                        await subscriptionId!.UnsubscribeAsync();
                    }
                },
                new EventLogSequenceNumberTokenWithFilter(State.SequenceNumber, State.EventTypes, _eventSourceId));
        }
    }

    /// <inheritdoc/>
    public async Task TryResume()
    {
        // Get the event log stream for this tenant
        // Use a stream sequence token that has partition in it
        // Subscribe to the stream with this token
        // When stream is at the edge - unsubscribe to the stream
        await Task.CompletedTask;
    }

    async Task HandleEvent(AppendedEvent @event, IEnumerable<EventType> eventTypes)
    {
        try
        {
            await _stream!.OnNextAsync(@event);
            State.IsFailed = false;
            await WriteStateAsync();
        }
        catch (Exception ex)
        {
            State.IsFailed = true;
            State.Occurred = DateTimeOffset.UtcNow;
            State.SequenceNumber = @event.Metadata.SequenceNumber;
            State.StackTrace = ex.StackTrace ?? string.Empty;
            State.EventTypes = eventTypes;
            State.Messages = GetMessagesFromException(ex);
            State.Attempts++;
            await WriteStateAsync();

            await HandleReminderRegistration();
        }
    }

    async Task HandleReminderRegistration()
    {
        if (State.Attempts <= 10)
        {
            _recoverReminder = await RegisterOrUpdateReminder(
                RecoverReminder,
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(60) * State.Attempts);
        }
        else
        {
            var reminder = await GetReminder(RecoverReminder);
            if (reminder != null)
            {
                await UnregisterReminder(reminder);
            }
        }
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
