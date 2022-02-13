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

            if (type.IsAssignableTo(typeof(IDictionary<string, object>)))
            {
                CompareDictionaryValues((left as IDictionary<string, object>)!, (right as IDictionary<string, object>)!, new PropertyPath(string.Empty), allDifferences);
            }
            else
            {
                ComparePropertiesFor(type, left, right, new PropertyPath(string.Empty), allDifferences);
            }

            return allDifferences.Count == 0;
        }

        void ComparePropertiesFor(Type type, object? left, object? right, PropertyPath currentPropertyPath, List<PropertyDifference> differences)
        {
            foreach (var property in type.GetProperties())
            {
                var propertyPath = currentPropertyPath + property.Name;
                var leftValue = property.GetValue(left);
                var rightValue = property.GetValue(right);

                CompareValues(property.PropertyType, leftValue, rightValue, propertyPath, differences);
            }
        }

        void CompareDictionaryValues(IDictionary<string, object> left, IDictionary<string, object> right, PropertyPath currentPropertyPath, List<PropertyDifference> differences)
        {
            var keys = left.Keys.ToList();
            keys.AddRange(right.Keys);

            foreach (var key in keys)
            {
                var propertyPath = currentPropertyPath + key;
                var leftValue = left!.ContainsKey(key) ? left[key] : null;
                var rightValue = right!.ContainsKey(key) ? right[key] : null;

                var type = leftValue?.GetType() ?? rightValue?.GetType();
                if (type is null)
                {
                    continue;
                }

                CompareValues(type, leftValue, rightValue, propertyPath, differences);
            }
        }

        void CompareValues(Type type, object? leftValue, object? rightValue, PropertyPath propertyPath, List<PropertyDifference> differences)
        {
            if (!type.IsPrimitive &&
              type != typeof(Guid) &&
              type != typeof(string) &&
              !type.IsConcept() &&
              !type.IsEnumerable())
            {
                ComparePropertiesFor(type, leftValue, rightValue, propertyPath, differences);
            }
            else if (type.IsAssignableTo(typeof(IDictionary<string, object>)))
            {
                CompareDictionaryValues((leftValue as IDictionary<string, object>)!, (rightValue as IDictionary<string, object>)!, propertyPath, differences);
            }
            else if (leftValue is not null && rightValue is not null && type.IsEnumerable())
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
                    var rightElements = rightValueAsEnumerable.ToObjectArray();

                    for (var i = 0; i < leftElements.Length; i++)
                    {
                        var elementDifferences = new List<PropertyDifference>();
                        CompareValues(leftElements[i].GetType(), leftElements[i], rightElements[i], propertyPath, elementDifferences);
                        if (elementDifferences.Count > 0)
                        {
                            differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
                            break;
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
