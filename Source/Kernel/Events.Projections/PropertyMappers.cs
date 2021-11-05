// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents utilities for creating <see cref="PropertyMapper"/> for different scenarios.
    /// </summary>
    public static class PropertyMappers
    {
        /// <summary>
        /// Create a <see cref="PropertyMapper"/> that can copies content provided by a <see cref="EventValueProvider"/> to a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="EventValueProvider"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static PropertyMapper FromEventValueProvider(Property targetProperty, EventValueProvider eventValueProvider)
        {
            return (Event @event, ExpandoObject target) =>
            {
                var actualTarget = target.MakeSurePathIsFulfilled(targetProperty) as IDictionary<string, object>;
                actualTarget[targetProperty.LastSegment] = eventValueProvider(@event);
            };
        }

        /// <summary>
        /// Create a <see cref="PropertyMapper"/> that can add a property with a value provided by a <see cref="EventValueProvider"/> onto a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="EventValueProvider"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static PropertyMapper AddWithEventValueProvider(Property targetProperty, EventValueProvider eventValueProvider)
        {
            return (Event @event, ExpandoObject target) =>
            {
                var lastSegment = targetProperty.LastSegment;
                var actualTarget = target.MakeSurePathIsFulfilled(targetProperty) as IDictionary<string, object>;
                if (!actualTarget.ContainsKey(lastSegment))
                {
                    actualTarget[lastSegment] = (double)0;
                }
                var value = (double)actualTarget[lastSegment];
                value += (double)eventValueProvider(@event);
                actualTarget[lastSegment] = value;
            };
        }

        /// <summary>
        /// Create a <see cref="PropertyMapper"/> that can add a property with a value provided by a <see cref="EventValueProvider"/> onto a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="EventValueProvider"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static PropertyMapper SubtractWithEventValueProvider(Property targetProperty, EventValueProvider eventValueProvider)
        {
            return (Event @event, ExpandoObject target) =>
            {
                var lastSegment = targetProperty.LastSegment;
                var actualTarget = target.MakeSurePathIsFulfilled(targetProperty) as IDictionary<string, object>;
                if (!actualTarget.ContainsKey(lastSegment))
                {
                    actualTarget[lastSegment] = (double)0;
                }
                var value = (double)actualTarget[lastSegment];
                value -= (double)eventValueProvider(@event);
                actualTarget[lastSegment] = value;
            };
        }
    }
}
