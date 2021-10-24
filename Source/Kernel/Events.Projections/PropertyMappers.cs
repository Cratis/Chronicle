// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Strings;

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
        public static PropertyMapper FromEventValueProvider(string targetProperty, EventValueProvider eventValueProvider)
        {
            var targetPath = GetTargetPath(targetProperty);

            return (Event @event, ExpandoObject target) =>
            {
                var actualTarget = GetActualTarget(target, targetPath);
                actualTarget[targetPath[^1]] = eventValueProvider(@event);
            };
        }

        /// <summary>
        /// Create a <see cref="PropertyMapper"/> that can add a property with a value provided by a <see cref="EventValueProvider"/> onto a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="EventValueProvider"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static PropertyMapper AddWithEventValueProvider(string targetProperty, EventValueProvider eventValueProvider)
        {
            var targetPath = GetTargetPath(targetProperty);

            return (Event @event, ExpandoObject target) =>
            {
                var actualTarget = GetActualTarget(target, targetPath);
                if (!actualTarget.ContainsKey(targetPath[^1]))
                {
                    actualTarget[targetPath[^1]] = (double)0;
                }
                var value = (double)actualTarget[targetPath[^1]];
                value += (double)eventValueProvider(@event);
                actualTarget[targetPath[^1]] = value;
            };
        }

        /// <summary>
        /// Create a <see cref="PropertyMapper"/> that can add a property with a value provided by a <see cref="EventValueProvider"/> onto a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="EventValueProvider"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static PropertyMapper SubtractWithEventValueProvider(string targetProperty, EventValueProvider eventValueProvider)
        {
            var targetPath = GetTargetPath(targetProperty);

            return (Event @event, ExpandoObject target) =>
            {
                var actualTarget = GetActualTarget(target, targetPath);
                if (!actualTarget.ContainsKey(targetPath[^1]))
                {
                    actualTarget[targetPath[^1]] = (double)0;
                }
                var value = (double)actualTarget[targetPath[^1]];
                value -= (double)eventValueProvider(@event);
                actualTarget[targetPath[^1]] = value;
            };
        }

        static string[] GetTargetPath(string targetProperty) => targetProperty.Split('.').Select(_ => _.ToCamelCase()).ToArray();

        static IDictionary<string, object> GetActualTarget(ExpandoObject target, string[] targetPath)
        {
            var currentTarget = target as IDictionary<string, object>;
            for (var propertyIndex = 0; propertyIndex < targetPath.Length - 1; propertyIndex++)
            {
                var property = targetPath[propertyIndex];
                if (!currentTarget.ContainsKey(property))
                {
                    var nested = new ExpandoObject();
                    currentTarget[property] = nested;
                    currentTarget = nested!;
                }
                else
                {
                    currentTarget = ((ExpandoObject)currentTarget[property])!;
                }
            }

            return currentTarget;
        }
    }
}
