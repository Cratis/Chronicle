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
        /// Create a <see cref="PropertyMapper"/> that can copies content based on a <see cref="EventValueProvider"/> to a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <param name="eventValueProvider"><see cref="EventValueProvider"/> to use as source.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static PropertyMapper FromEventValueProvider(string targetProperty, EventValueProvider eventValueProvider)
        {
            var targetPath = targetProperty.Split('.');

            return (Event @event, ExpandoObject target) =>
            {
                var currentTarget = target as IDictionary<string, object>;
                for (var propertyIndex = 0; propertyIndex < targetPath.Length - 1; propertyIndex++)
                {
                    var property = targetPath[propertyIndex];
                    if (!currentTarget.ContainsKey(property))
                    {
                        var nested = new ExpandoObject();
                        currentTarget[property] = nested;
                        currentTarget = nested as IDictionary<string, object>;
                    }
                    else
                    {
                        currentTarget = ((ExpandoObject)currentTarget[property])!;
                    }
                }
                currentTarget[targetPath[^1]] = eventValueProvider(@event);
            };
        }
    }
}
