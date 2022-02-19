// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections
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
        public static PropertyMapper<AppendedEvent, ExpandoObject> FromEventValueProvider(PropertyPath targetProperty, ValueProvider<AppendedEvent> eventValueProvider)
        {
            return (AppendedEvent @event, ExpandoObject target, IArrayIndexers arrayIndexers) =>
            {
                var actualTarget = target.EnsurePath(targetProperty, arrayIndexers) as IDictionary<string, object>;
                actualTarget[targetProperty.LastSegment.Value] = eventValueProvider(@event);
            };
        }

        /// <summary>
        /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can add a property with a value provided by a <see cref="ValueProvider{Event}"/> onto a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="ValueProvider{Event}"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
        public static PropertyMapper<AppendedEvent, ExpandoObject> AddWithEventValueProvider(PropertyPath targetProperty, ValueProvider<AppendedEvent> eventValueProvider)
        {
            return (AppendedEvent @event, ExpandoObject target, IArrayIndexers arrayIndexers) =>
            {
                var lastSegment = targetProperty.LastSegment;
                var actualTarget = target.EnsurePath(targetProperty, arrayIndexers) as IDictionary<string, object>;
                if (!actualTarget.ContainsKey(lastSegment.Value))
                {
                    actualTarget[lastSegment.Value] = 0D;
                }
                var value = (double)Convert.ChangeType(actualTarget[lastSegment.Value], typeof(double), CultureInfo.InvariantCulture);
                value += (double)Convert.ChangeType(eventValueProvider(@event), typeof(double), CultureInfo.InvariantCulture);
                actualTarget[lastSegment.Value] = value;
            };
        }

        /// <summary>
        /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can add a property with a value provided by a <see cref="ValueProvider{Event}"/> onto a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="ValueProvider{Event}"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
        public static PropertyMapper<AppendedEvent, ExpandoObject> SubtractWithEventValueProvider(PropertyPath targetProperty, ValueProvider<AppendedEvent> eventValueProvider)
        {
            return (AppendedEvent @event, ExpandoObject target, IArrayIndexers arrayIndexers) =>
            {
                var lastSegment = targetProperty.LastSegment;
                var actualTarget = target.EnsurePath(targetProperty, arrayIndexers) as IDictionary<string, object>;
                if (!actualTarget.ContainsKey(lastSegment.Value))
                {
                    actualTarget[lastSegment.Value] = 0D;
                }
                var value = (double)Convert.ChangeType(actualTarget[lastSegment.Value], typeof(double), CultureInfo.InvariantCulture);
                value -= (double)Convert.ChangeType(eventValueProvider(@event), typeof(double), CultureInfo.InvariantCulture);
                actualTarget[lastSegment.Value] = value;
            };
        }
    }
}
