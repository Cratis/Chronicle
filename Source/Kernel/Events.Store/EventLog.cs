// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInversion;
using Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLog"/>.
    /// </summary>
    public class EventLog : Grain, IEventLog
    {
        readonly IPersistentState<EventLogState> _state;
        readonly ProviderFor<IEventLogs> _eventLogsProvider;
        readonly ILogger<EventLog> _logger;
        EventLogId _eventLogId = EventLogId.Unspecified;
        TenantId _tenantId = TenantId.NotSet;

        /// <summary>
        /// Initializes a new instance of <see cref="EventLog"/>.
        /// </summary>
        /// <param name="state">State of the grain.</param>
        /// <param name="eventLogsProvider"><see cref="IEventLogs"/> for storage.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public EventLog(
            [PersistentState("EventLog", EventLogState.StorageProvider)] IPersistentState<EventLogState> state,
            ProviderFor<IEventLogs> eventLogsProvider,
            ILogger<EventLog> logger)
        {
            _state = state;
            _eventLogsProvider = eventLogsProvider;
            _logger = logger;
        }

        /// <inheritdoc/>
        public override Task OnActivateAsync()
        {
            _eventLogId = this.GetPrimaryKey(out var tenantIdAsString);
            _tenantId = Guid.Parse(tenantIdAsString);
            return base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task Append(EventSourceId eventSourceId, EventType eventType, string content)
        {
            _logger.Appending(eventType, eventSourceId, _state.State.SequenceNumber, _eventLogId);

            await _eventLogsProvider().Append(_eventLogId, _state.State.SequenceNumber, eventSourceId, eventType, content);

            var appendedEvent = new AppendedEvent(
                new EventMetadata(_state.State.SequenceNumber, eventType),
                new EventContext(eventSourceId, DateTimeOffset.UtcNow),
                content
            );

            _state.State.SequenceNumber++;
            await _state.WriteStateAsync();

            var streamProvider = GetStreamProvider("event-log");
            var stream = streamProvider.GetStream<AppendedEvent>(Guid.Empty, "greetings");
            var handlers = await stream.GetAllSubscriptionHandles();
            await stream.OnNextAsync(appendedEvent); //, new EventSequenceToken(_state.State.SequenceNumber));

            var observers = GrainFactory.GetGrain<IEventLogObservers>(_eventLogId, keyExtension: _tenantId.ToString());
            await observers.Next(appendedEvent);
        }

        /// <inheritdoc/>
        public async Task Compensate(EventLogSequenceNumber sequenceNumber, EventType eventType, string content, DateTimeOffset? validFrom = default)
        {
            _logger.Compensating(eventType, sequenceNumber, _eventLogId);

            await _eventLogsProvider().Compensate(_eventLogId, sequenceNumber, eventType, content);

            // Notify observers
        }
    }
}
