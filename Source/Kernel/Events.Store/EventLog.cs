// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Providers.Streams.Common;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLog"/>.
    /// </summary>
    [StorageProvider(ProviderName = EventLogState.StorageProvider)]
    public class EventLog : Grain<EventLogState>, IEventLog
    {
        public const string StreamProvider = "event-log";
        readonly ILogger<EventLog> _logger;
        EventLogId _eventLogId = EventLogId.Unspecified;
        TenantId _tenantId = TenantId.NotSet;

        /// <summary>
        /// Initializes a new instance of <see cref="EventLog"/>.
        /// </summary>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public EventLog(
            ILogger<EventLog> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public override Task OnActivateAsync()
        {
            _eventLogId = this.GetPrimaryKey(out var tenantId);
            _tenantId = tenantId;
            return base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task Append(EventSourceId eventSourceId, EventType eventType, string content)
        {
            _logger.Appending(eventType, eventSourceId, State.SequenceNumber, _eventLogId);

            var appendedEvent = new AppendedEvent(
                new EventMetadata(State.SequenceNumber, eventType),
                new EventContext(eventSourceId, DateTimeOffset.UtcNow),
                content
            );

            State.SequenceNumber++;
            await WriteStateAsync();

            var streamProvider = GetStreamProvider(StreamProvider);
            var stream = streamProvider.GetStream<AppendedEvent>(_eventLogId, _tenantId.ToString());
            await stream.OnNextAsync(appendedEvent, new EventSequenceToken(State.SequenceNumber));
        }

        /// <inheritdoc/>
        public Task Compensate(EventLogSequenceNumber sequenceNumber, EventType eventType, string content, DateTimeOffset? validFrom = default)
        {
            _logger.Compensating(eventType, sequenceNumber, _eventLogId);

            return Task.CompletedTask;
        }
    }
}
