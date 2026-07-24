// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Strings;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a serializer for appended events.
/// </summary>
/// <remarks>
/// Runs on the Orleans serialization hot path for every <see cref="AppendedEvent"/> that crosses a grain or
/// silo boundary. Schemas are cached per event type and generation on the write side and per unique schema
/// text on the read side, so the blocking event type schema lookup and the schema parse only happen on the
/// first occurrence of each event type per silo.
/// </remarks>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
internal sealed class AppendedEventSerializer(
    JsonSerializerOptions jsonSerializerOptions,
    IExpandoObjectConverter expandoObjectConverter,
    IStorage storage) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    readonly ConcurrentDictionary<string, JsonSchema> _schemasBySchemaJson = new();
    readonly ConcurrentDictionary<EventTypeSchemaKey, string> _schemaJsonByEventType = new();

    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context) => input;

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => type == typeof(AppendedEvent);

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (type == typeof(AppendedEvent))
        {
            return true;
        }
        return null;
    }

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        var json = StringCodec.ReadValue(ref reader, field);
        var jsonObject = JsonNode.Parse(json)!;
        var appendedEventWithSchema = JsonSerializer.Deserialize<AppendedEventWithSchema>(json, jsonSerializerOptions)!;
        var appendedEventJson = jsonObject[nameof(AppendedEventWithSchema.AppendedEvent).ToCamelCase()]!;
        var content = appendedEventJson[nameof(AppendedEvent.Content).ToCamelCase()];
        var contentAsJson = (JsonObject)content!;
        var schema = _schemasBySchemaJson.GetOrAdd(appendedEventWithSchema.Schema, JsonSchema.FromJson);
        var contentAsExpando = expandoObjectConverter.ToExpandoObject(contentAsJson, schema);
        return appendedEventWithSchema.AppendedEvent with { Content = contentAsExpando };
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        var appendedEvent = (AppendedEvent)value;
        var key = new EventTypeSchemaKey(appendedEvent.Context.EventStore, appendedEvent.Context.EventType.Id, appendedEvent.Context.EventType.Generation);
        var schemaJson = _schemaJsonByEventType.GetOrAdd(key, GetSchemaJson);
        var appendedEventWithSchema = new AppendedEventWithSchema(appendedEvent, schemaJson);

        var json = JsonSerializer.Serialize(appendedEventWithSchema, jsonSerializerOptions);
        StringCodec.WriteField(ref writer, fieldIdDelta, json);
    }

    string GetSchemaJson(EventTypeSchemaKey key)
    {
        var eventStore = storage.GetEventStore(key.EventStore);
        var eventType = eventStore.EventTypes.GetFor(key.EventTypeId, key.Generation).GetAwaiter().GetResult();
        return eventType.Schema.ToJson();
    }

    readonly record struct EventTypeSchemaKey(EventStoreName EventStore, EventTypeId EventTypeId, EventTypeGeneration Generation);

    sealed record AppendedEventWithSchema(AppendedEvent AppendedEvent, string Schema);
}
