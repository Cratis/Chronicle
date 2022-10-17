// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Json;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents utilities for creating <see cref="ValueProvider{T}"/> instances for providing values from <see cref="AppendedEvent">events</see>.
/// </summary>
public static class EventValueProviders
{
    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that provides the event source id from an event.
    /// </summary>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static readonly ValueProvider<AppendedEvent> EventSourceId = (AppendedEvent @event) => @event.Context.EventSourceId.ToString();

    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that provides a value from the event content.
    /// </summary>
    /// <param name="sourceProperty">Source property.</param>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static ValueProvider<AppendedEvent> EventContent(PropertyPath sourceProperty)
    {
        return (AppendedEvent @event) => GetValueFromEventContent(@event, sourceProperty);
    }

    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that provides a composite value based on multiple properties in the event content.
    /// </summary>
    /// <param name="sourceProperties">Source properties.</param>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static ValueProvider<AppendedEvent> EventContentComposite(IEnumerable<PropertyPath> sourceProperties)
    {
        return (AppendedEvent @event) =>
        {
            var builder = new StringBuilder();
            foreach (var property in sourceProperties)
            {
                if (builder.Length > 0)
                {
                    builder.Append("+");
                }
                builder.Append(GetValueFromEventContent(@event, property));
            }
            return builder.ToString();
        };
    }

    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that provides a value from the <see cref="Store.EventContext"/>.
    /// </summary>
    /// <param name="sourceProperty">Property on the context.</param>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static ValueProvider<AppendedEvent> EventContext(PropertyPath sourceProperty)
    {
        var property = sourceProperty.GetPropertyInfoFor<EventContext>();
        return (AppendedEvent @event) => property.GetValue(@event.Context)!;
    }

    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that generates a new unique identifier from the event metadata.
    /// </summary>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static ValueProvider<AppendedEvent> UniqueIdentifier() => (AppendedEvent @event) => $"{@event.Metadata.SequenceNumber}-{@event.Context.Occurred.ToUnixTimeMilliseconds()}";

    static object GetValueFromEventContent(AppendedEvent @event, PropertyPath sourceProperty)
    {
        JsonNode? currentSource = @event.Content;
        object? sourceValue = null;
        foreach (var property in sourceProperty.Segments)
        {
            var value = currentSource![property.Value];
            if (value is JsonValue jsonValue)
            {
                jsonValue.TryGetValue(out sourceValue);

                if (sourceValue is JsonElement element)
                {
                    switch (element.ValueKind)
                    {
                        case JsonValueKind.True:
                            sourceValue = true;
                            break;
                        case JsonValueKind.False:
                            sourceValue = false;
                            break;
                        default:
                            element.TryGetValue(out sourceValue);
                            break;
                    }
                }
            }
            else if (value is JsonObject jsonObject)
            {
                sourceValue = jsonObject.AsExpandoObject();
            }
            currentSource = value;
        }

        return sourceValue!;
    }
}
