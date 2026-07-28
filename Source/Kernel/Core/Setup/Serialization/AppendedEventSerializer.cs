// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
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
/// silo boundary. The write side resolves the schema through the silo-wide <see cref="IEventTypeSchemaCache"/>,
/// which is evicted whenever an event type is re-registered; the read side caches the parsed schema per unique
/// schema text, which needs no eviction since a changed schema is a different key.
/// </remarks>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/>.</param>
/// <param name="schemaCache">The <see cref="IEventTypeSchemaCache"/> to resolve event type schemas from.</param>
internal sealed class AppendedEventSerializer(
    JsonSerializerOptions jsonSerializerOptions,
    IExpandoObjectConverter expandoObjectConverter,
    IEventTypeSchemaCache schemaCache) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    readonly ConcurrentDictionary<string, JsonSchema> _schemasBySchemaJson = new();

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
        var schemaJson = schemaCache.GetSchemaJsonFor(
            appendedEvent.Context.EventStore,
            appendedEvent.Context.EventType.Id,
            appendedEvent.Context.EventType.Generation);
        var appendedEventWithSchema = new AppendedEventWithSchema(appendedEvent, schemaJson);

        var json = JsonSerializer.Serialize(appendedEventWithSchema, jsonSerializerOptions);
        StringCodec.WriteField(ref writer, fieldIdDelta, json);
    }

    sealed record AppendedEventWithSchema(AppendedEvent AppendedEvent, string Schema);
}
