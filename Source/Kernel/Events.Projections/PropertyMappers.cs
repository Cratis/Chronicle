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
        /// Create a <see cref="PropertyMapper"/> that can copy the content of a property from within the content of an event to a target property.
        /// </summary>
        /// <param name="sourceProperty">Source property.</param>
        /// <param name="targetProperty">Target property.</param>
        /// <returns>A new <see cref="PropertyMapper"/>.</returns>
        public static PropertyMapper FromEventContent(string sourceProperty, string targetProperty)
        {
            var propertyMapperExpression = Expression.Assign(
                Expression.Property(_targetParameter, _itemProperty, Expression.Constant(targetProperty)),
                Expression.Property(
                    _contentMember,
                    _itemProperty,
                    Expression.Constant(sourceProperty)));

            return Expression.Lambda<PropertyMapper>(propertyMapperExpression, new[] {
                _eventParameter,
                _targetParameter
            }).Compile();
        }
    }
}
