// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequences"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
/// <param name="storage"><see cref="IStorage"/> for storing events.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
public class EventSequences(
    IGrainFactory grainFactory,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions) : IEventSequences
{
    /// <inheritdoc/>
    public async Task<AppendResponse> Append(AppendRequest request, CallContext context = default)
    {
        var eventSequence = GetEventSequenceGrain(request);
        var result = await eventSequence.Append(
            request.EventSource,
            request.EventSourceId,
            request.EventStreamType,
            request.EventStreamId,
            request.EventType.ToChronicle(),
            JsonSerializer.Deserialize<JsonNode>(request.Content, jsonSerializerOptions)!.AsObject(),
            request.CorrelationId,
            request.Causation.ToChronicle(),
            request.CausedBy.ToChronicle());

        return result.ToContract();
    }

    /// <inheritdoc/>
    public async Task<AppendManyResponse> AppendMany(AppendManyRequest request, CallContext context = default)
    {
        var eventSequence = GetEventSequenceGrain(request);
        var result = await eventSequence.AppendMany(
            request.Events.ToChronicle(),
            request.CorrelationId,
            request.Causation.ToChronicle(),
            request.CausedBy.ToChronicle());

        return result.ToContract();
    }

    /// <inheritdoc/>
    public async Task<GetForEventSourceIdAndEventTypesResponse> GetForEventSourceIdAndEventTypes(GetForEventSourceIdAndEventTypesRequest request, CallContext context = default)
    {
        var eventSequence = GetEventSequenceStorage(request);

        var cursor = await eventSequence.GetFromSequenceNumber(
            EventSequenceNumber.First,
            request.EventSourceId,
            request.EventStreamType,
            request.EventStreamId,
            request.EventTypes.ToChronicle());

        var events = new List<Contracts.Events.AppendedEvent>();
        while (await cursor.MoveNext())
        {
            var current = cursor.Current;
            events.AddRange(current.ToContract());
        }
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
    public async Task<GetFromEventSequenceNumberResponse> GetEventsFromEventSequenceNumber(
        GetFromEventSequenceNumberRequest request,
        CallContext context = default)
    {
        var eventSequence = GetEventSequenceStorage(request);

        var cursor = await eventSequence.GetFromSequenceNumber(
            request.EventSequenceNumber,
            string.IsNullOrWhiteSpace(request.EventSourceId) ? (EventSourceId)null! : request.EventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            request.EventTypes.ToChronicle());

        var events = new List<Contracts.Events.AppendedEvent>();
        while (await cursor.MoveNext())
        {
            var current = cursor.Current;
            events.AddRange(current.ToContract());
        }
        return new()
        {
            Events = events
        };
    }

    IEventSequenceStorage GetEventSequenceStorage(IEventSequenceRequest request) =>
        storage.GetEventStore(request.EventStoreName).GetNamespace(request.Namespace).GetEventSequence(request.EventSequenceId);

    Grains.EventSequences.IEventSequence GetEventSequenceGrain(IEventSequenceRequest request) =>
        grainFactory.GetGrain<Grains.EventSequences.IEventSequence>(new EventSequenceKey(request.EventSequenceId, request.EventStoreName, request.Namespace));
}
