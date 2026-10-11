// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventTypes"/>.
/// </summary>
public class EventTypesForSpecifications : IEventTypes
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    static readonly JsonSchemaExporterOptions _exporterOptions = new()
    {
        TreatNullObliviousAsNonNullable = true
    };

    readonly Dictionary<Type, EventType> _eventTypes = [];
    readonly Dictionary<EventType, Type> _clrTypesByEventType = [];
    readonly Dictionary<EventType, JsonSchema> _jsonSchemasByEventType = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypesForSpecifications"/> class.
    /// </summary>
    /// <param name="types"><see cref="IEnumerable{T}"/> of <see cref="Type"/> representing the event types.</param>
    public EventTypesForSpecifications(IEnumerable<Type>? types = null)
    {
        types ??= [];

        _eventTypes = types.ToDictionary(_ => _, _ => _.GetEventType());
        _clrTypesByEventType = _eventTypes.ToDictionary(_ => _.Value, _ => _.Key);

        _jsonSchemasByEventType = _eventTypes.ToDictionary(
            _ => _.Value,
            _ => new JsonSchema(_serializerOptions.GetJsonSchemaAsNode(_.Key, _exporterOptions).AsObject()));

        AllClrTypes = _clrTypesByEventType.Values.ToImmutableList();
        All = _eventTypes.Values.ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<Type> AllClrTypes { get; }

    /// <inheritdoc/>
    public IImmutableList<EventType> All { get; }

    /// <inheritdoc/>
    public Task Discover() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Register() => Task.CompletedTask;

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => _clrTypesByEventType
        .Where(_ => _.Key.Id == eventTypeId)
        .OrderByDescending(_ => _.Key.Generation.Value)
        .First().Value;

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId, EventTypeGeneration generation) => _clrTypesByEventType
        .Where(_ => _.Key.Id == eventTypeId && _.Key.Generation == generation)
        .Select(_ => _.Value)
        .FirstOrDefault() ?? GetClrTypeFor(eventTypeId);

    /// <inheritdoc/>
    /// <remarks>
    /// Answers a type that is not an event type the way the real <c language="csharp">EventTypes</c> does, so that a spec measuring
    /// what a caller does with the miss measures the real failure rather than a dictionary lookup.
    /// </remarks>
    public EventType GetEventTypeFor(Type clrType)
    {
        EnsureEventType(clrType);
        return _eventTypes.TryGetValue(clrType, out var eventType) ? eventType : throw new TypeIsNotAnEventType(clrType);
    }

    /// <inheritdoc/>
    public JsonSchema GetSchemaFor(EventTypeId eventTypeId) => _jsonSchemasByEventType
        .Where(_ => _.Key.Id == eventTypeId)
        .OrderByDescending(_ => _.Key.Generation.Value)
        .First().Value;

    /// <inheritdoc/>
    public bool HasFor(EventTypeId eventTypeId) => _clrTypesByEventType.Any(_ => _.Key.Id == eventTypeId);

    /// <inheritdoc/>
    public bool HasFor(Type clrType)
    {
        EnsureEventType(clrType);
        return _eventTypes.ContainsKey(clrType);
    }

    void EnsureEventType(Type clrType)
    {
        if (!_eventTypes.ContainsKey(clrType))
        {
            var eventTypeAttribute = clrType.GetCustomAttribute<EventTypeAttribute>();
            if (eventTypeAttribute is not null)
            {
                var eventType = clrType.GetEventType();
                _eventTypes[clrType] = eventType;
                _clrTypesByEventType[eventType] = clrType;
            }
        }
    }
}
