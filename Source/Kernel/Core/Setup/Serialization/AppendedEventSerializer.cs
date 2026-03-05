// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Cratis.Strings;
using NJsonSchema;
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
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
internal sealed class AppendedEventSerializer(
    JsonSerializerOptions jsonSerializerOptions,
    IExpandoObjectConverter expandoObjectConverter,
    IStorage storage) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
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
        var schema = JsonSchema.FromJsonAsync(appendedEventWithSchema.Schema).GetAwaiter().GetResult();
        var contentAsExpando = expandoObjectConverter.ToExpandoObject(contentAsJson, schema);
        return appendedEventWithSchema.AppendedEvent with { Content = contentAsExpando };
    }

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value)
        where TBufferWriter : IBufferWriter<byte>
    {
        var appendedEvent = (AppendedEvent)value;
        var eventStore = storage.GetEventStore(appendedEvent.Context.EventStore);
        var eventType = eventStore.EventTypes.GetFor(appendedEvent.Context.EventType.Id, appendedEvent.Context.EventType.Generation).GetAwaiter().GetResult();
        var appendedEventWithSchema = new AppendedEventWithSchema(appendedEvent, eventType.Schema.ToJson());

        var json = JsonSerializer.Serialize(appendedEventWithSchema, jsonSerializerOptions);
        StringCodec.WriteField(ref writer, fieldIdDelta, json);
    }

    sealed record AppendedEventWithSchema(AppendedEvent AppendedEvent, string Schema);
}
