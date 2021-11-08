// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents utilities for creating <see cref="EventValueProvider"/> instances for different scenarios.
    /// </summary>
    public static class EventValueProviders
    {
        /// <summary>
        /// Create a <see cref="PropertyMapper"/> that can copy the content of the events event source id from within the content of an event to a target property.
        /// </summary>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static readonly EventValueProvider FromEventSourceId = (Event @event) => @event.EventSourceId.ToString();

        /// <summary>
        /// Create a <see cref="PropertyMapper"/> that can copy the content of a property from within the content of an event to a target property.
        /// </summary>
        /// <param name="sourceProperty">Source property.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static EventValueProvider FromEventContent(string sourceProperty)
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
