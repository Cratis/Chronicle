// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Produces public copies of event content and revision requests with webhook target header values omitted.
/// Internal event processing retains the original content.
/// </summary>
internal static class PublicEventContentRedactor
{
    const string InvalidContent = "Invalid public event content.";
    static readonly EventTypeId WebhookAddedType = typeof(WebhookAdded).GetEventType().Id;
    static readonly EventTypeId TargetHeadersSetType = typeof(TargetHeadersSetForWebhook).GetEventType().Id;
    static readonly EventTypeId EventRevisedType = typeof(EventRevised).GetEventType().Id;

    /// <summary>
    /// Creates a detached public copy of a header-bearing event, including its content history.
    /// </summary>
    /// <param name="appendedEvent">The event to present.</param>
    /// <param name="options">The content serialization options.</param>
    /// <returns>The public copy, or the original event when no redaction is required.</returns>
    public static AppendedEvent Redact(AppendedEvent appendedEvent, JsonSerializerOptions options)
    {
        if (!RequiresRedaction(appendedEvent.Context.EventType))
        {
            return appendedEvent;
        }

        return appendedEvent with
        {
            Content = RedactContent(appendedEvent.Context.EventType, appendedEvent.Content, options),
            OriginalContent = Redact(appendedEvent.Context.EventType, appendedEvent.OriginalContent, options),
            Revisions = appendedEvent.Revisions.Select(revision => revision with { Content = Redact(appendedEvent.Context.EventType, revision.Content, options) }).ToArray(),
            GenerationalContent = appendedEvent.GenerationalContent.ToDictionary(pair => pair.Key, pair => Redact(appendedEvent.Context.EventType, pair.Value, options))
        };
    }

    /// <summary>
    /// Redacts a serialized content variant without changing unrelated event types.
    /// </summary>
    /// <param name="eventType">The event type, independent of its generation or event store.</param>
    /// <param name="content">The serialized content, or an empty optional history value.</param>
    /// <param name="options">The content serialization options.</param>
    /// <returns>The public content.</returns>
    public static string Redact(EventType eventType, string content, JsonSerializerOptions options)
    {
        if (!RequiresRedaction(eventType) || string.IsNullOrEmpty(content))
        {
            return content;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException(InvalidContent);
            }

            var revisionContent = eventType.Id == EventRevisedType ? RedactRevision(document.RootElement, options) : null;
            if (eventType.Id == EventRevisedType && revisionContent is null)
            {
                return content;
            }

            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = options.Encoder, Indented = options.WriteIndented });
            WriteContent(writer, document.RootElement, revisionContent);
            writer.Flush();
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
        catch (JsonException)
        {
            throw new JsonException(InvalidContent);
        }
    }

    static ExpandoObject RedactContent(EventType eventType, ExpandoObject content, JsonSerializerOptions options)
    {
        if (eventType.Id != EventRevisedType)
        {
            return RedactContent(content);
        }

        try
        {
            var revisionContent = RedactRevision(JsonSerializer.SerializeToElement(content, options), options);
            if (revisionContent is null)
            {
                return content;
            }

            IDictionary<string, object?> copy = new ExpandoObject();
            foreach (var property in (IDictionary<string, object?>)content)
            {
                copy[property.Key] = IsContent(property.Key) ? revisionContent : property.Value;
            }

            return ((ExpandoObject)copy).Clone();
        }
        catch (JsonException)
        {
            throw new JsonException(InvalidContent);
        }
    }

    static string? RedactRevision(JsonElement envelope, JsonSerializerOptions options)
    {
        var type = RequiredProperty(envelope, "eventType");
        var id = RequiredString(RequiredProperty(type, "id"));
        var eventType = new EventType(id, EventTypeGeneration.First);
        if (!RequiresRedaction(eventType))
        {
            return null;
        }

        var content = RequiredString(RequiredProperty(envelope, "content"));
        return Redact(eventType, content, options);
    }

    static string RequiredString(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(value.GetString()))
        {
            throw new JsonException(InvalidContent);
        }

        return value.GetString()!;
    }

    static JsonElement RequiredProperty(JsonElement content, string name)
    {
        if (content.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(InvalidContent);
        }

        var properties = content.EnumerateObject().Where(property => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (properties.Length != 1)
        {
            throw new JsonException(InvalidContent);
        }

        return properties[0].Value;
    }

    static ExpandoObject RedactContent(ExpandoObject content)
    {
        IDictionary<string, object?> copy = new ExpandoObject();
        foreach (var property in (IDictionary<string, object?>)content)
        {
            copy[property.Key] = IsTargetHeaders(property.Key) ? RedactHeaders(property.Value) : property.Value;
        }

        return ((ExpandoObject)copy).Clone();
    }

    static ExpandoObject RedactHeaders(object? headers)
    {
        IDictionary<string, object?> copy = new ExpandoObject();
        foreach (var name in HeaderNames(headers))
        {
            copy[name] = string.Empty;
        }

        return (ExpandoObject)copy;
    }

    static IEnumerable<string> HeaderNames(object? headers) => headers switch
    {
        JsonElement { ValueKind: JsonValueKind.Object } json => json.EnumerateObject().Select(property => property.Name),
        JsonObject json => json.Select(property => property.Key),
        _ => DictionaryHeaderNames(headers)
    };

    static IEnumerable<string> DictionaryHeaderNames(object? headers) => headers switch
    {
        IDictionary<string, object?> dictionary => dictionary.Keys,
        IReadOnlyDictionary<string, object?> dictionary => dictionary.Keys,
        IReadOnlyDictionary<string, string> dictionary => dictionary.Keys,
        IDictionary dictionary => dictionary.Keys.OfType<string>(),
        _ => []
    };

    static bool RequiresRedaction(EventType eventType) =>
        eventType.Id == WebhookAddedType || eventType.Id == TargetHeadersSetType || eventType.Id == EventRevisedType;

    static bool IsTargetHeaders(string name) => name.Equals("targetHeaders", StringComparison.OrdinalIgnoreCase);

    static bool IsContent(string name) => name.Equals("content", StringComparison.OrdinalIgnoreCase);

    static void WriteContent(Utf8JsonWriter writer, JsonElement content, string? revisionContent)
    {
        writer.WriteStartObject();
        foreach (var property in content.EnumerateObject())
        {
            writer.WritePropertyName(property.Name);
            if (revisionContent is not null && IsContent(property.Name))
            {
                writer.WriteStringValue(revisionContent);
            }
            else if (revisionContent is null && IsTargetHeaders(property.Name))
            {
                WriteHeaders(writer, property.Value);
            }
            else
            {
                property.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }

    static void WriteHeaders(Utf8JsonWriter writer, JsonElement headers)
    {
        writer.WriteStartObject();
        if (headers.ValueKind == JsonValueKind.Object)
        {
            foreach (var header in headers.EnumerateObject())
            {
                writer.WriteString(header.Name, string.Empty);
            }
        }

        writer.WriteEndObject();
    }
}
