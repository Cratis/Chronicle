// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// Builds the pinned public JSON shape using the kernel's content converters.
/// </summary>
/// <param name="options">The kernel JSON serializer options.</param>
[Singleton]
public class DeliveryEnvelopeWriter(JsonSerializerOptions options) : IDeliveryEnvelopeWriter
{
    readonly JsonSerializerOptions _options = CreateOptions(options);

    /// <inheritdoc/>
    public DeliveryEnvelope ForEvents(HttpDeliveryRequest request)
    {
        var partition = request.Partition.ToString();
        var events = request.Events.Select(@event => DeliveredEvent.From(@event, DeliveryIdentity.Event(request.EventStore, request.Namespace, request.ObserverId, request.EventSequenceId, @event.Context.SequenceNumber))).ToArray();
        var first = request.Events.FirstOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable;
        var last = request.Events.LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable;
        var id = DeliveryIdentity.Delivery(request.EventStore, request.Namespace, request.ObserverId, request.EventSequenceId, DeliveryKind.Events, [(partition, first.Value, last.Value)]);
        return new(DeliveryKind.Events, request.ObserverType, request.ObserverId, request.EventStore, request.Namespace, request.EventSequenceId, id, [new(partition, events)]);
    }

    /// <inheritdoc/>
    public DeliveryEnvelope Control(DeliveryKind kind, HttpDeliveryRequest request)
    {
        if (kind == DeliveryKind.Events)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var id = DeliveryIdentity.Delivery(request.EventStore, request.Namespace, request.ObserverId, request.EventSequenceId, kind, []);
        return new(kind, request.ObserverType, request.ObserverId, request.EventStore, request.Namespace, request.EventSequenceId, id, []);
    }

    /// <inheritdoc/>
    public byte[] Serialize(DeliveryEnvelope envelope) => JsonSerializer.SerializeToUtf8Bytes(envelope, _options);

    static JsonSerializerOptions CreateOptions(JsonSerializerOptions options)
    {
        var result = new JsonSerializerOptions(options)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        result.Converters.Insert(0, new JsonStringEnumConverter<DeliveryKind>());
        result.Converters.Insert(0, new JsonStringEnumConverter<EventObservationState>(JsonNamingPolicy.CamelCase));
        return result;
    }
}
