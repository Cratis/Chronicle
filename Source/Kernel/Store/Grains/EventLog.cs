// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLog"/>.
    /// </summary>
    [StorageProvider(ProviderName = EventLogState.StorageProvider)]
    public class EventLog : Grain<EventLogState>, IEventLog
    {
        /// <summary>
        /// The name of the stream provider.
        /// </summary>
        public const string StreamProvider = "event-log";
        readonly ISchemaStore _schemaStore;
        readonly IJsonComplianceManager _jsonComplianceManager;
        readonly ILogger<EventLog> _logger;
        EventLogId _eventLogId = EventLogId.Unspecified;
        TenantId _tenantId = TenantId.NotSet;
        IAsyncStream<AppendedEvent>? _stream;

        /// <summary>
        /// Initializes a new instance of <see cref="EventLog"/>.
        /// </summary>
        /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
        /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public EventLog(
            ISchemaStore schemaStore,
            IJsonComplianceManager jsonComplianceManager,
            ILogger<EventLog> logger)
        {
            _schemaStore = schemaStore;
            _jsonComplianceManager = jsonComplianceManager;
            _logger = logger;
        }

        /// <inheritdoc/>
        public override async Task OnActivateAsync()
        {
            _eventLogId = this.GetPrimaryKey(out var tenantId);
            _tenantId = tenantId;

            var streamProvider = GetStreamProvider(StreamProvider);
            _stream = streamProvider.GetStream<AppendedEvent>(_eventLogId, _tenantId.ToString());

            await base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task WarmUp()
        {
            var appendedEvent = new AppendedEvent(
                new EventMetadata(0, new EventType(Guid.Empty, EventGeneration.First)),
                new EventContext(string.Empty, DateTimeOffset.UtcNow),
                "{}");

            await _stream!.OnNextAsync(appendedEvent, new EventLogSequenceNumberToken());
            await WriteStateAsync();
        }

        /// <inheritdoc/>
        public async Task Append(EventSourceId eventSourceId, EventType eventType, string content)
        {
            _logger.Appending(eventType, eventSourceId, State.SequenceNumber, _eventLogId);

            var updateSequenceNumber = true;
            try
            {
                var eventSchema = await _schemaStore.GetFor(eventType.Id, eventType.Generation);
                var compliantEvent = await _jsonComplianceManager.Apply(eventSchema.Schema, eventSourceId, JObject.Parse(content));

                var appendedEvent = new AppendedEvent(
                    new EventMetadata(State.SequenceNumber, eventType),
                    new EventContext(eventSourceId, DateTimeOffset.UtcNow),
                    compliantEvent.ToString());

                await _stream!.OnNextAsync(appendedEvent, new EventLogSequenceNumberToken(State.SequenceNumber));
            }
            catch (UnableToAppendToEventLog ex)
            {
                _logger.FailedAppending(
                    ex.StreamId,
                    ex.SequenceNumber,
                    ex.TenantId,
                    ex.EventSourceId,
                    ex);

                updateSequenceNumber = false;
            }
            catch (Exception ex)
            {
                _logger.ErrorAppending(
                    _eventLogId,
                    State.SequenceNumber,
                    _tenantId,
                    eventSourceId,
                    ex);
            }

            if (updateSequenceNumber)
            {
                State.SequenceNumber++;
                await WriteStateAsync();
            }
        }

        /// <inheritdoc/>
        public Task Compensate(EventLogSequenceNumber sequenceNumber, EventType eventType, string content, DateTimeOffset? validFrom = default)
        {
            _logger.Compensating(eventType, sequenceNumber, _eventLogId);

            return Task.CompletedTask;
        }
    }
}
