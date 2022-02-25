// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Store.EventLogs;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>.
/// </summary>
[StorageProvider(ProviderName = EventSequenceState.StorageProvider)]
public class EventSequence : Grain<EventSequenceState>, IEventSequence
{
    /// <summary>
    /// The name of the stream provider.
    /// </summary>
    public const string StreamProvider = "event-sequence";
    readonly ISchemaStore _schemaStore;
    readonly IJsonComplianceManager _jsonComplianceManager;
    readonly ILogger<EventSequence> _logger;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    MicroserviceAndTenant _microserviceAndTenant = MicroserviceAndTenant.NotSet;
    IAsyncStream<AppendedEvent>? _stream;

    /// <summary>
    /// Initializes a new instance of <see cref="EventSequence"/>.
    /// </summary>
    /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
    /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public EventSequence(
        ISchemaStore schemaStore,
        IJsonComplianceManager jsonComplianceManager,
        ILogger<EventSequence> logger)
    {
        _schemaStore = schemaStore;
        _jsonComplianceManager = jsonComplianceManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _eventSequenceId = this.GetPrimaryKey(out var extension);
        _microserviceAndTenant = MicroserviceAndTenant.Parse(extension);

        var streamProvider = GetStreamProvider(StreamProvider);
        _stream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, extension);

        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, EventType eventType, JsonObject content)
    {
        _logger.Appending(
            _microserviceAndTenant.MicroserviceId,
            _microserviceAndTenant.TenantId,
            _eventSequenceId,
            eventType,
            eventSourceId,
            State.SequenceNumber);

        var updateSequenceNumber = true;
        try
        {
            var eventSchema = await _schemaStore.GetFor(eventType.Id, eventType.Generation);
            var compliantEvent = await _jsonComplianceManager.Apply(eventSchema.Schema, eventSourceId, content);

            var appendedEvent = new AppendedEvent(
                new EventMetadata(State.SequenceNumber, eventType),
                new EventContext(eventSourceId, DateTimeOffset.UtcNow),
                compliantEvent);

            await _stream!.OnNextAsync(appendedEvent, new EventLogSequenceNumberToken(State.SequenceNumber));
        }
        catch (UnableToAppendToEventSequence ex)
        {
            _logger.FailedAppending(
                _microserviceAndTenant.MicroserviceId,
                _microserviceAndTenant.TenantId,
                ex.StreamId,
                ex.EventSourceId,
                ex.SequenceNumber,
                ex);

            updateSequenceNumber = false;
        }
        catch (Exception ex)
        {
            _logger.ErrorAppending(
                _microserviceAndTenant.MicroserviceId,
                _microserviceAndTenant.TenantId,
                _eventSequenceId,
                eventSourceId,
                State.SequenceNumber,
                ex);
        }

        if (updateSequenceNumber)
        {
            State.SequenceNumber++;
            await WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    public Task Compensate(EventSequenceNumber sequenceNumber, EventType eventType, string content, DateTimeOffset? validFrom = default)
    {
        _logger.Compensating(
            _microserviceAndTenant.MicroserviceId,
            _microserviceAndTenant.TenantId,
            eventType,
            _eventSequenceId,
            sequenceNumber);

        return Task.CompletedTask;
    }
}
