// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Strings;
using Microsoft.Extensions.Options;
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
/// <param name="configuration">The configured cluster wire format.</param>
internal sealed class AppendedEventSerializer(
    JsonSerializerOptions jsonSerializerOptions,
    IExpandoObjectConverter expandoObjectConverter,
    IEventTypeSchemaCache schemaCache,
    IOptions<ChronicleOptions> configuration) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    readonly ConcurrentDictionary<string, JsonSchema> _schemasBySchemaJson = new();
    readonly CompactAppendedEventCodec _compact = new(jsonSerializerOptions);

    /// <summary>
    /// Initializes a serializer using the released JSON writer by default.
    /// </summary>
    /// <param name="jsonSerializerOptions">The existing JSON options.</param>
    /// <param name="expandoObjectConverter">The normalized-content converter.</param>
    /// <param name="schemaCache">The schema cache.</param>
    public AppendedEventSerializer(
        JsonSerializerOptions jsonSerializerOptions,
        IExpandoObjectConverter expandoObjectConverter,
        IEventTypeSchemaCache schemaCache)
        : this(jsonSerializerOptions, expandoObjectConverter, schemaCache, Options.Create(new ChronicleOptions()))
    {
    }

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
        if (field.WireType == WireType.Reference)
        {
            var referenced = ReferenceCodec.ReadReference<object, TInput>(ref reader, field);
            if (referenced is AppendedEvent or null)
            {
                return referenced!;
            }
            return ReadLegacyJson((string)referenced);
        }
        if (field.WireType == WireType.TagDelimited)
        {
            return _compact.ReadValue(ref reader, field)!;
        }
        return ReadLegacy(ref reader, field);
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, [AllowNull] Type expectedType, [AllowNull] object? value)
        where TBufferWriter : IBufferWriter<byte>
    {
        var appendedEvent = (AppendedEvent)value!;
        if (configuration.Value.Clustering.EventWireFormat == EventWireFormat.Compact &&
            _compact.TryWrite(ref writer, fieldIdDelta, expectedType, appendedEvent))
        {
            return;
        }
        WriteLegacy(ref writer, fieldIdDelta, appendedEvent);
    }

    void WriteLegacy<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, AppendedEvent appendedEvent)
        where TBufferWriter : IBufferWriter<byte>
    {
        var schemaJson = schemaCache.GetSchemaJsonFor(
            appendedEvent.Context.EventStore,
            appendedEvent.Context.EventType.Id,
            appendedEvent.Context.EventType.Generation);
        var appendedEventWithSchema = new AppendedEventWithSchema(appendedEvent, schemaJson);

        var json = JsonSerializer.Serialize(appendedEventWithSchema, jsonSerializerOptions);
        StringCodec.WriteField(ref writer, fieldIdDelta, json);
    }

    object ReadLegacy<TInput>(ref Reader<TInput> reader, Field field)
    {
        var json = StringCodec.ReadValue(ref reader, field)!;
        return ReadLegacyJson(json);
    }

    object ReadLegacyJson(string json)
    {
        var jsonObject = JsonNode.Parse(json)!;
        var appendedEventWithSchema = JsonSerializer.Deserialize<AppendedEventWithSchema>(json, jsonSerializerOptions)!;
        var appendedEventJson = jsonObject[nameof(AppendedEventWithSchema.AppendedEvent).ToCamelCase()]!;
        var content = appendedEventJson[nameof(AppendedEvent.Content).ToCamelCase()];
        var contentAsJson = (JsonObject)content!;
        var schema = _schemasBySchemaJson.GetOrAdd(appendedEventWithSchema.Schema, JsonSchema.FromJson);
        var contentAsExpando = expandoObjectConverter.ToExpandoObject(contentAsJson, schema);
        return appendedEventWithSchema.AppendedEvent with { Content = contentAsExpando };
    }

    sealed record AppendedEventWithSchema(AppendedEvent AppendedEvent, string Schema);
}
