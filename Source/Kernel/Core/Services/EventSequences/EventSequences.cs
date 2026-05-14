// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Services.Auditing;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Services.EventSequences.Concurrency;
using Cratis.Chronicle.Services.Identities;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using ProtoBuf.Grpc;
using static Cratis.Chronicle.EventSequences.EventSequencesGrainFactoryExtensions;

namespace Cratis.Chronicle.Services.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequences"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
/// <param name="storage"><see cref="IStorage"/> for storing events.</param>
/// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for decrypting PII event content.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between expando objects and JSON.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
internal sealed class EventSequences(
    IGrainFactory grainFactory,
    IStorage storage,
    IJsonComplianceManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions) : IEventSequences
{
    /// <inheritdoc/>
    public async Task<AppendResponse> Append(AppendRequest request, CallContext context = default)
    {
        var eventSequence = GetEventSequenceGrain(request);
        var result = await eventSequence.Append(
            request.EventSourceType,
            request.EventSourceId,
            request.EventStreamType,
            request.EventStreamId,
            request.EventType.ToChronicle(),
            JsonSerializer.Deserialize<JsonNode>(request.Content, jsonSerializerOptions)!.AsObject(),
            request.CorrelationId,
            request.Causation.ToChronicle(),
            request.CausedBy.ToChronicle(),
            request.Tags.Select(t => new Tag(t)).ToArray(),
            request.ConcurrencyScope.ToChronicle(),
            request.Occurred,
            string.IsNullOrEmpty(request.Subject) ? null : new Subject(request.Subject));

        return result.ToContract();
    }

    /// <inheritdoc/>
    public async Task<AppendManyResponse> AppendMany(AppendManyRequest request, CallContext context = default)
    {
        var eventSequence = GetEventSequenceGrain(request);
        var result = await eventSequence.AppendMany(
            request.Events.ToChronicle(jsonSerializerOptions),
            request.CorrelationId,
            request.Causation.ToChronicle(),
            request.CausedBy.ToChronicle(),
            request.ConcurrencyScopes.ToChronicle());

        return result.ToContract();
    }

    /// <inheritdoc/>
    public async Task<GetTailSequenceNumberResponse> GetTailSequenceNumber(GetTailSequenceNumberRequest request, CallContext context = default)
    {
        var eventSequence = GetEventSequenceStorage(request);
        var tail = await eventSequence.GetTailSequenceNumber(
            request.EventTypes.ToChronicle(),
            request.EventSourceId is null ? null : (EventSourceId)request.EventSourceId,
            request.EventSourceType is null ? null : (EventSourceType)request.EventSourceType,
            request.EventStreamId is null ? null : (EventStreamId)request.EventStreamId,
            request.EventStreamType is null ? null : (EventStreamType)request.EventStreamType);

        return new() { SequenceNumber = tail };
    }

    /// <inheritdoc/>
    public async Task<GetForEventSourceIdAndEventTypesResponse> GetForEventSourceIdAndEventTypes(GetForEventSourceIdAndEventTypesRequest request, CallContext context = default)
    {
        var eventSequence = GetEventSequenceStorage(request);

        using var cursor = await eventSequence.GetFromSequenceNumber(
            EventSequenceNumber.First,
            request.EventSourceId,
            request.EventStreamType,
            request.EventStreamId,
            request.EventTypes.ToChronicle());

        var appendedEvents = new List<AppendedEvent>();
        while (await cursor.MoveNext())
        {
            appendedEvents.AddRange(cursor.Current);
        }

        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetFor(appendedEvents.Select(_ => _.Context.EventType).Distinct());
        var schemasByEventType = eventTypeSchemas.ToDictionary(_ => _.Type);
        var events = await ToContracts(appendedEvents, schemasByEventType);

        return new()
        {
            Events = events
        };
    }

    /// <inheritdoc/>
    public async Task<HasEventsForEventSourceIdResponse> HasEventsForEventSourceId(HasEventsForEventSourceIdRequest request, CallContext context = default)
    {
        var eventSequence = GetEventSequenceStorage(request);
        return new()
        {
            HasEvents = await eventSequence.HasEventsFor(request.EventSourceId)
        };
    }

    /// <inheritdoc/>
    public async Task Revise(ReviseRequest request, CallContext context = default)
    {
        var systemEventSequence = grainFactory.GetSystemEventSequence(request.EventStore, request.Namespace);
        await systemEventSequence.Append(
            (EventSourceId)request.EventSequenceId,
            new EventRevised(
                request.EventSequenceId,
                request.SequenceNumber,
                request.EventType.ToChronicle(),
                request.Content),
            correlationId: request.CorrelationId,
            causation: request.Causation.ToChronicle(),
            causedBy: request.CausedBy.ToChronicle());
    }

    /// <inheritdoc/>
    public async Task<GetFromEventSequenceNumberResponse> GetEventsFromEventSequenceNumber(
        GetFromEventSequenceNumberRequest request,
        CallContext context = default)
    {
        var eventSequence = GetEventSequenceStorage(request);

        IEventCursor cursor;

        if (request.ToEventSequenceNumber is not null)
        {
            cursor = await eventSequence.GetRange(
                request.FromEventSequenceNumber,
                request.ToEventSequenceNumber,
                string.IsNullOrWhiteSpace(request.EventSourceId) ? (EventSourceId)null! : request.EventSourceId,
                request.EventTypes.ToChronicle());
        }
        else
        {
            cursor = await eventSequence.GetFromSequenceNumber(
                request.FromEventSequenceNumber,
                string.IsNullOrWhiteSpace(request.EventSourceId) ? (EventSourceId)null! : request.EventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                request.EventTypes.ToChronicle());
        }

        var appendedEvents = new List<AppendedEvent>();
        while (await cursor.MoveNext())
        {
            appendedEvents.AddRange(cursor.Current);
        }

        cursor.Dispose();
        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetFor(appendedEvents.Select(_ => _.Context.EventType).Distinct());
        var schemasByEventType = eventTypeSchemas.ToDictionary(_ => _.Type);
        var events = await ToContracts(appendedEvents, schemasByEventType);

        return new()
        {
            Events = events
        };
    }

    /// <inheritdoc/>
    public async Task<RedactResponse> Redact(RedactRequest request, CallContext context = default)
    {
        var systemEventSequence = grainFactory.GetSystemEventSequence(request.EventStore, request.Namespace);
        await systemEventSequence.Append(
            (EventSourceId)request.EventSequenceId,
            new EventRedactionRequested(
                request.EventSequenceId,
                request.SequenceNumber,
                request.Reason),
            correlationId: request.CorrelationId,
            causation: request.Causation.ToChronicle(),
            causedBy: request.CausedBy.ToChronicle());

        return new RedactResponse();
    }

    /// <inheritdoc/>
    public async Task RedactForEventSource(RedactForEventSourceRequest request, CallContext context = default)
    {
        var systemEventSequence = grainFactory.GetSystemEventSequence(request.EventStore, request.Namespace);
        await systemEventSequence.Append(
            (EventSourceId)request.EventSequenceId,
            new EventsRedactedForEventSource(
                request.EventSequenceId,
                (EventSourceId)request.EventSourceId,
                request.EventTypes.ToChronicle(),
                (RedactionReason)request.Reason),
            correlationId: request.CorrelationId,
            causation: request.Causation.ToChronicle(),
            causedBy: request.CausedBy.ToChronicle());
    }

    async Task<IList<Contracts.Events.AppendedEvent>> ToContracts(
        IEnumerable<AppendedEvent> events,
        Dictionary<EventType, EventTypeSchema> schemasByEventType)
    {
        var contracts = new List<Contracts.Events.AppendedEvent>();
        foreach (var @event in events)
        {
            if (!schemasByEventType.TryGetValue(@event.Context.EventType, out var schema) ||
                !schema.Schema.HasComplianceMetadata())
            {
                contracts.Add(@event.ToContract(jsonSerializerOptions));
                continue;
            }

            var releasedEvent = await EventComplianceHelper.ReleaseEventContent(
                complianceManager,
                expandoObjectConverter,
                @event,
                schema.Schema);
            contracts.Add(releasedEvent.ToContract(jsonSerializerOptions));
        }

        return contracts;
    }

    IEventSequenceStorage GetEventSequenceStorage(IEventSequenceRequest request) =>
        storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).GetEventSequence(request.EventSequenceId);

    Chronicle.EventSequences.IEventSequence GetEventSequenceGrain(IEventSequenceRequest request) =>
        grainFactory.GetGrain<Chronicle.EventSequences.IEventSequence>(new EventSequenceKey(request.EventSequenceId, request.EventStore, request.Namespace));
}
