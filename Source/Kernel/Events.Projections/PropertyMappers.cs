// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents utilities for creating <see cref="PropertyMapper"/> for different scenarios.
    /// </summary>
    public class PropertyMappers
    {
        static readonly ParameterExpression _eventParameter = Expression.Parameter(typeof(Event));
        static readonly ParameterExpression _targetParameter = Expression.Parameter(typeof(ExpandoObject));
        static readonly MemberExpression _contentMember = Expression.Property(_eventParameter, typeof(Event), nameof(Event.Content));
        static readonly PropertyInfo _itemProperty = typeof(IDictionary<string, object>).GetProperty("Item")!;

        /// <summary>
        /// Create a <see cref="PropertyMapper"/> that can copy the content of the events event source id from within the content of an event to a target property.
        /// </summary>
        /// <param name="targetProperty">Target property.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static PropertyMapper FromEventSourceId(string targetProperty)
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
                currentTarget[targetPath[^1]] = @event.EventSourceId;
            };
        }

        /// <summary>
        /// Create a <see cref="PropertyMapper"/> that can copy the content of a property from within the content of an event to a target property.
        /// </summary>
        /// <param name="sourceProperty">Source property.</param>
        /// <param name="targetProperty">Target property.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static PropertyMapper FromEventContent(string sourceProperty, string targetProperty)
        {
            var sourcePath = sourceProperty.Split('.');
            var targetPath = targetProperty.Split('.');

            return (Event @event, ExpandoObject target) =>
            {
                var currentSource = @event.Content as IDictionary<string, object>;
                object? sourceValue = null;
                foreach (var property in sourcePath)
                {
                    sourceValue = currentSource![property];
                    currentSource = sourceValue as IDictionary<string, object>;
                }

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
                currentTarget[targetPath[^1]] = sourceValue!;
            };
        }
    }
}
