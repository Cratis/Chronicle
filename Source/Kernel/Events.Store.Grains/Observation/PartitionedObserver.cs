// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Observation;
using Cratis.Execution;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IPartitionedObserver"/>.
    /// </summary>
    [StorageProvider(ProviderName = PartitionedObserverState.StorageProvider)]
    public class PartitionedObserver : Grain<PartitionedObserverState>, IPartitionedObserver, IRemindable
    {
        const string RecoverReminder = "partitioned-observer-failure-recovery";

        ObserverId _observerId = ObserverId.Unspecified;
        IAsyncStream<AppendedEvent>? _stream;
        IGrainReminder? _recoverReminder;
        TenantId _tenantId = TenantId.NotSet;
        EventSourceId _eventSourceId = EventSourceId.Unspecified;

        /// <inheritdoc/>
        public override async Task OnActivateAsync()
        {
            _observerId = this.GetPrimaryKey(out var key);
            var (tenantId, eventSourceId) = PartitionedObserverKeyHelper.Parse(key);
            _tenantId = tenantId;
            _eventSourceId = eventSourceId;

            var streamProvider = GetStreamProvider("observer-handlers");
            _stream = streamProvider.GetStream<AppendedEvent>(_observerId, null);

            _recoverReminder = await GetReminder(RecoverReminder);
            if (_recoverReminder != default && !State.IsFailed)
            {
                await UnregisterReminder(_recoverReminder);
            }

            await base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public Task SetEventTypes(IEnumerable<EventType> eventTypes)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task OnNext(AppendedEvent @event)
        {
            if (State.IsFailed)
            {
                return;
            }

            try
            {
                await _stream!.OnNextAsync(@event);
            }
            catch (Exception ex)
            {
                State.IsFailed = true;
                State.Occurred = DateTimeOffset.UtcNow;
                State.SequenceNumber = @event.Metadata.SequenceNumber;
                State.StackTrace = ex.StackTrace ?? string.Empty;

                var messages = new List<string>
                {
                    ex.Message
                };

                while (ex.InnerException != null)
                {
                    messages.Insert(0, ex.InnerException.Message);
                    ex = ex.InnerException;
                }

                State.Messages = messages.ToArray();
                await WriteStateAsync();
                _recoverReminder = await RegisterOrUpdateReminder(RecoverReminder, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));
            }
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            // TODO: Create a back off type reminder and stop after X number of attempts.
            if (reminderName == RecoverReminder)
            {
                var reminder = await GetReminder(RecoverReminder);

                if (reminder == default)
                {
                    return;
                }

                var streamProvider = GetStreamProvider(EventLog.StreamProvider);

                // TODO: Put event log id as part of grain identity
                _stream = streamProvider.GetStream<AppendedEvent>(Guid.Empty, _tenantId.ToString());

                await _stream.SubscribeAsync(
                    async (@event, _) =>
                    {
                        State.IsFailed = false;
                        await OnNext(@event);

                        // TODO: If we've failed and a reminder has been added, break this subscription and unsubscribe
                        if (!State.IsFailed)
                        {
                            State.IsFailed = false;
                            await WriteStateAsync();
                        }
                    }, new EventLogSequenceNumberTokenWithFilter(State.SequenceNumber, Array.Empty<EventType>(), _eventSourceId));

                // TODO: Track subscription handle - when recovered, unsubscribe.
            }
        }

        public async Task TryResume()
        {
            // Get the event log stream for this tenant
            // Use a stream sequence token that has partition in it
            // Subscribe to the stream with this token
            // When stream is at the edge - unsubscribe to the stream

            await Task.CompletedTask;
        }
    }
}
