// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Dynamic;
using Cratis.Properties;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents utilities for creating <see cref="PropertyMapper{Event, ExpandoObject}"/> for different scenarios.
    /// </summary>
    public static class PropertyMappers
    {
        /// <summary>
        /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can copies content provided by a <see cref="ValueProvider{Event}"/> to a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="ValueProvider{Event}"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
        public static PropertyMapper<Event, ExpandoObject> FromEventValueProvider(PropertyPath targetProperty, ValueProvider<Event> eventValueProvider)
        {
            return (Event @event, ExpandoObject target) =>
            {
                var actualTarget = target.EnsurePath(targetProperty) as IDictionary<string, object>;
                actualTarget[targetProperty.LastSegment] = eventValueProvider(@event);
            };
        }

        /// <summary>
        /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can add a property with a value provided by a <see cref="ValueProvider{Event}"/> onto a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="ValueProvider{Event}"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
        public static PropertyMapper<Event, ExpandoObject> AddWithEventValueProvider(PropertyPath targetProperty, ValueProvider<Event> eventValueProvider)
        {
            return (Event @event, ExpandoObject target) =>
            {
                var lastSegment = targetProperty.LastSegment;
                var actualTarget = target.EnsurePath(targetProperty) as IDictionary<string, object>;
                if (!actualTarget.ContainsKey(lastSegment))
                {
                    actualTarget[lastSegment] = (double)0;
                }
                var value = (double)Convert.ChangeType(actualTarget[lastSegment], typeof(double), CultureInfo.InvariantCulture);
                value += (double)Convert.ChangeType(eventValueProvider(@event), typeof(double), CultureInfo.InvariantCulture);
                actualTarget[lastSegment] = value;
            };
        }

        /// <summary>
        /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can add a property with a value provided by a <see cref="ValueProvider{Event}"/> onto a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="ValueProvider{Event}"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
        public static PropertyMapper<Event, ExpandoObject> SubtractWithEventValueProvider(PropertyPath targetProperty, ValueProvider<Event> eventValueProvider)
        {
            return (Event @event, ExpandoObject target) =>
            {
                var lastSegment = targetProperty.LastSegment;
                var actualTarget = target.EnsurePath(targetProperty) as IDictionary<string, object>;
                if (!actualTarget.ContainsKey(lastSegment))
                {
                    actualTarget[lastSegment] = (double)0;
                }
                var value = (double)Convert.ChangeType(actualTarget[lastSegment], typeof(double), CultureInfo.InvariantCulture);
                value -= (double)Convert.ChangeType(eventValueProvider(@event), typeof(double), CultureInfo.InvariantCulture);
                actualTarget[lastSegment] = value;
            };
        }
    }
}
