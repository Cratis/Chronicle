// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents utilities for creating <see cref="ValueProvider{T}"/> instances for providing values from <see cref="AppendedEvent">events</see>.
    /// </summary>
    public static class EventValueProviders
    {
        /// <summary>
        /// Create a <see cref="ValueProvider{T}"/> that can copy the content of the events event source id from within the content of an event to a target property.
        /// </summary>
        /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
        public static readonly ValueProvider<AppendedEvent> FromEventSourceId = (AppendedEvent @event) => @event.Context.EventSourceId.ToString();

        /// <summary>
        /// Create a <see cref="ValueProvider{T}"/> that can copy the content of a property from within the content of an event to a target property.
        /// </summary>
        /// <param name="sourceProperty">Source property.</param>
        /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
        public static ValueProvider<AppendedEvent> FromEventContent(PropertyPath sourceProperty)
        {
            return (AppendedEvent @event) =>
            {
                JsonNode? currentSource = @event.Content;
                object? sourceValue = null;
                foreach (var property in sourceProperty.Segments)
                {
                    var value = currentSource![property];
                    if (value is JsonValue jsonValue)
                    {
                        jsonValue.TryGetValue(out sourceValue);
                    }
                    currentSource = value;
                }

                return sourceValue!;
            };
        }

        /// <summary>
        /// Create a <see cref="ValueProvider{T}"/> that generates a new unique identifier from the event metadata.
        /// </summary>
        /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
        public static ValueProvider<AppendedEvent> UniqueIdentifier() => (AppendedEvent @event) => $"{@event.Metadata.SequenceNumber}-{@event.Context.Occurred.ToUnixTimeMilliseconds()}";
    }
}
