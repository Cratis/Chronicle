// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Aksio.Cratis.Collections;
using Aksio.Cratis.Concepts;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Changes
{
    /// <summary>
    /// Represents an implementation of <see cref="IObjectsComparer"/>.
    /// </summary>
    public class ObjectsComparer : IObjectsComparer
    {
        /// <inheritdoc/>
        public bool Equals(object? left, object? right, out IEnumerable<PropertyDifference> differences)
        {
            var allDifferences = new List<PropertyDifference>();
            differences = allDifferences;

            var type = left?.GetType() ?? right?.GetType();
            if (type is null)
            {
                return true;
            }

            ComparePropertiesFor(type, left, right, new PropertyPath(string.Empty), allDifferences);

            return allDifferences.Count == 0;
        }

        void ComparePropertiesFor(Type type, object? left, object? right, PropertyPath currentPropertyPath, List<PropertyDifference> differences)
        {
            foreach (var property in type.GetProperties())
            {
                var propertyPath = currentPropertyPath + property.Name;
                var leftValue = property.GetValue(left);
                var rightValue = property.GetValue(right);

                CompareValues(propertyPath, property.PropertyType, leftValue, rightValue, differences);
            }
        }

        void CompareValues(PropertyPath propertyPath, Type type, object? leftValue, object? rightValue, List<PropertyDifference> differences)
        {
            if (!type.IsPrimitive &&
              type != typeof(Guid) &&
              type != typeof(string) &&
              !type.IsConcept() &&
              !type.IsEnumerable())
            {
                ComparePropertiesFor(type, leftValue, rightValue, propertyPath, differences);
            }
            else if (leftValue is not null && rightValue is not null && type.IsEnumerable())
            {
                if (type.IsEnumerable())
                {
                    var leftValueAsEnumerable = (leftValue as IEnumerable)!;
                    var rightValueAsEnumerable = (rightValue as IEnumerable)!;
                    if (leftValueAsEnumerable.CountElements() != rightValueAsEnumerable.CountElements())
                    {
                        differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
                    }
                    else
                    {
                        var leftElements = leftValueAsEnumerable.ToObjectArray();
                        var rightElements = leftValueAsEnumerable.ToObjectArray();

                        for (var i = 0; i < leftElements.Length; i++)
                        {
                            var elementDifferences = new List<PropertyDifference>();
                            CompareValues(propertyPath, leftElements[i].GetType(), leftElements[i], rightElements[i], elementDifferences);
                            if (elementDifferences.Count > 0)
                            {
                                differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
                                break;
                            }
                        }
                    }
                }
            }
            else if ((leftValue is null && rightValue is not null) ||
              (leftValue is not null && rightValue is null) ||
              !leftValue!.Equals(rightValue))
            {
                differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
            }
        }
    }
}
