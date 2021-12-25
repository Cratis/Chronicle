// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLog"/>.
    /// </summary>
    public class EventLog : Grain, IEventLog
    {
        public const string StorageProvider = "event-log";

        readonly IPersistentState<EventLogState> _state;
        readonly ILogger<EventLog> _logger;
        EventLogId _eventLogId = EventLogId.Unspecified;

        /// <summary>
        /// Initializes a new instance of <see cref="EventLog"/>.
        /// </summary>
        /// <param name="state">State of the grain.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public EventLog(
            [PersistentState("EventLog", EventLogState.StorageProvider)] IPersistentState<EventLogState> state,
            ILogger<EventLog> logger)
        {
            _state = state;
            _logger = logger;
        }

        /// <inheritdoc/>
        public override Task OnActivateAsync()
        {
            _eventLogId = this.GetPrimaryKey(out var _);
            return base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task Append(EventSourceId eventSourceId, EventType eventType, string content)
        {
            _logger.Appending(eventType, eventSourceId, _state.State.SequenceNumber, _eventLogId);

            var appendedEvent = new AppendedEvent(
                new EventMetadata(_state.State.SequenceNumber, eventType),
                new EventContext(eventSourceId, DateTimeOffset.UtcNow),
                content
            );

            _state.State.SequenceNumber++;
            await _state.WriteStateAsync();

            var streamProvider = GetStreamProvider(StorageProvider);
            var stream = streamProvider.GetStream<AppendedEvent>(_eventLogId, null);
            await stream.OnNextAsync(appendedEvent, new EventSequenceToken(_state.State.SequenceNumber));
        }

        /// <inheritdoc/>
        public Task Compensate(EventLogSequenceNumber sequenceNumber, EventType eventType, string content, DateTimeOffset? validFrom = default)
        {
            _logger.Compensating(eventType, sequenceNumber, _eventLogId);

            return Task.CompletedTask;
        }
    }
}
