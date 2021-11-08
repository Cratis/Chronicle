// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Properties;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents utilities for creating <see cref="ValueProvider{T}"/> instances for providing values from <see cref="Event">events</see>.
    /// </summary>
    public static class EventValueProviders
    {
        /// <summary>
        /// Create a <see cref="ValueProvider{T}"/> that can copy the content of the events event source id from within the content of an event to a target property.
        /// </summary>
        /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
        public static readonly ValueProvider<Event> FromEventSourceId = (Event @event) => @event.EventSourceId.ToString();

        /// <summary>
        /// Create a <see cref="ValueProvider{T}"/> that can copy the content of a property from within the content of an event to a target property.
        /// </summary>
        /// <param name="sourceProperty">Source property.</param>
        /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
        public static ValueProvider<Event> FromEventContent(string sourceProperty)
        {
            var sourcePath = sourceProperty.Split('.');

            return (Event @event) =>
            {
                var currentSource = @event.Content as IDictionary<string, object>;
                object? sourceValue = null;
                foreach (var property in sourcePath)
                {
                    sourceValue = currentSource![property];
                    currentSource = sourceValue as IDictionary<string, object>;
                }

                return sourceValue!;
            };
        }
    }
}
