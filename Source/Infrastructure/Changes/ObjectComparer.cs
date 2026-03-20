// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Reflection;
using Cratis.Chronicle.Properties;
using Cratis.Collections;
using Cratis.Concepts;
using Cratis.DependencyInjection;
using Cratis.Reflection;
using OneOf;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Represents an implementation of <see cref="IObjectComparer"/>.
/// </summary>
[Singleton]
public class ObjectComparer : IObjectComparer
{
    /// <inheritdoc/>
    public bool Compare(object? left, object? right, out IEnumerable<PropertyDifference> differences) =>
        Compare(left, right, ObjectComparerMode.Strict, out differences);

    /// <inheritdoc/>
    public bool Compare(object? left, object? right, ObjectComparerMode mode, out IEnumerable<PropertyDifference> differences)
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
            CompareDictionaryValues((left as IDictionary<string, object>)!, (right as IDictionary<string, object>)!, new PropertyPath(string.Empty), mode, allDifferences);
        }
        else
        {
            ComparePropertiesFor(type, left, right, new PropertyPath(string.Empty), mode, allDifferences);
        }

        return allDifferences.Count == 0;
    }

    void ComparePropertiesFor(Type type, object? left, object? right, PropertyPath currentPropertyPath, ObjectComparerMode mode, List<PropertyDifference> differences)
    {
        foreach (var property in type.GetProperties())
        {
            var leftValue = left != null ? property.GetValue(left) : null;
            var rightValue = right != null ? property.GetValue(right) : null;
            if (leftValue is null && rightValue is null) continue;

            var propertyPath = currentPropertyPath + (type.IsEnumerable() ? $"[{property.Name}]" : property.Name);
            CompareValues(property.PropertyType, leftValue, rightValue, propertyPath, mode, differences);
        }
    }

    void CompareDictionaryValues(IDictionary<string, object> left, IDictionary<string, object> right, PropertyPath currentPropertyPath, ObjectComparerMode mode, List<PropertyDifference> differences)
    {
        left ??= new Dictionary<string, object>();
        right ??= new Dictionary<string, object>();

        var keys = left.Keys.ToList();
        keys.AddRange(right.Keys);

        foreach (var key in keys.Distinct())
        {
            if (!left.TryGetValue(key, out var leftValue))
            {
                leftValue = null;
            }
            if (!right.TryGetValue(key, out var rightValue))
            {
                rightValue = null;
            }

            var type = leftValue?.GetType() ?? rightValue?.GetType();
            if (type is null)
            {
                continue;
            }

            var propertyPath = currentPropertyPath.AddProperty(key, type);
            CompareValues(type, leftValue, rightValue, propertyPath, mode, differences);
        }
    }

    void CompareDictionaryValues(IEnumerable left, IEnumerable right, PropertyPath currentPropertyPath, ObjectComparerMode mode, List<PropertyDifference> differences)
    {
        var leftDictionary = left.GetKeyValuePairs().ToDictionary(_ => _.Key, _ => _.Value);
        var rightDictionary = right.GetKeyValuePairs().ToDictionary(_ => _.Key, _ => _.Value);

        var keys = leftDictionary.Keys.ToList();
        keys.AddRange(rightDictionary.Keys);

        foreach (var key in keys.Distinct())
        {
            if (!leftDictionary.TryGetValue(key, out var leftValue))
            {
                leftValue = null;
            }
            if (!rightDictionary.TryGetValue(key, out var rightValue))
            {
                rightValue = null;
            }

            var type = leftValue?.GetType() ?? rightValue?.GetType();
            if (type is null)
            {
                continue;
            }

            var propertyPath = currentPropertyPath.AddProperty(key, type);
            CompareValues(type, leftValue, rightValue, propertyPath, mode, differences);
        }
    }

    void CompareEnumerableValues(Type type, object leftValue, object rightValue, PropertyPath propertyPath, ObjectComparerMode mode, List<PropertyDifference> differences)
    {
        var leftValueAsEnumerable = (leftValue as IEnumerable)!;
        var rightValueAsEnumerable = (rightValue as IEnumerable)!;
        if (leftValueAsEnumerable.CountElements() != rightValueAsEnumerable.CountElements())
        {
            differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
            return;
        }

        var leftElements = leftValueAsEnumerable.ToObjectArray();
        var rightElements = rightValueAsEnumerable.ToObjectArray();

        if (mode == ObjectComparerMode.Loose)
        {
            CompareEnumerableValuesLoose(type, leftValue, rightValue, leftElements, rightElements, propertyPath, mode, differences);
            return;
        }

        CompareEnumerableValuesStrict(leftValueAsEnumerable, rightValueAsEnumerable, leftElements, rightElements, propertyPath, mode, differences);
    }

    void CompareEnumerableValuesStrict(IEnumerable leftValueAsEnumerable, IEnumerable rightValueAsEnumerable, object[] leftElements, object[] rightElements, PropertyPath propertyPath, ObjectComparerMode mode, List<PropertyDifference> differences)
    {
        var leftElementType = leftValueAsEnumerable.GetType().GetElementType();
        var rightElementType = rightValueAsEnumerable.GetType().GetElementType();

        if (leftElementType == rightElementType &&
            (leftElementType?.IsPrimitive == true || leftElementType == typeof(string)))
        {
            for (var i = 0; i < leftElements.Length; i++)
            {
                var elementDifferences = new List<PropertyDifference>();
                CompareValues(leftElementType, leftElements[i], rightElements[i], propertyPath, mode, elementDifferences);
                differences.AddRange(elementDifferences);

                if (elementDifferences.Count > 0) break;
            }
        }
        else
        {
            for (var i = 0; i < leftElements.Length; i++)
            {
                var elementDifferences = new List<PropertyDifference>();
                CompareValues(
                    leftElements[i]?.GetType() ?? rightElements[i]?.GetType() ?? typeof(object),
                    leftElements[i],
                    rightElements[i],
                    propertyPath,
                    mode,
                    elementDifferences);
                differences.AddRange(elementDifferences);

                if (elementDifferences.Count > 0) break;
            }
        }
    }

    void CompareEnumerableValuesLoose(object leftValue, object rightValue, object[] leftElements, object[] rightElements, PropertyPath propertyPath, ObjectComparerMode mode, List<PropertyDifference> differences)
    {
        // In loose mode, every element from the left must have a matching element in the right (and vice versa),
        // regardless of position.
        var rightMatched = new bool[rightElements.Length];

        foreach (var leftElement in leftElements)
        {
            var found = false;
            for (var j = 0; j < rightElements.Length; j++)
            {
                if (rightMatched[j]) continue;

                var elementDifferences = new List<PropertyDifference>();
                var elementType = leftElement?.GetType() ?? rightElements[j]?.GetType() ?? typeof(object);
                CompareValues(elementType, leftElement, rightElements[j], propertyPath, mode, elementDifferences);

                if (elementDifferences.Count == 0)
                {
                    rightMatched[j] = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
                return;
            }
        }
    }

    void CompareValues(Type type, object? leftValue, object? rightValue, PropertyPath propertyPath, ObjectComparerMode mode, List<PropertyDifference> differences)
    {
        if (leftValue is null && rightValue is null) return;

        if ((leftValue is null && rightValue is not null) ||
          (leftValue is not null && rightValue is null))
        {
            differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
        }
        else if (type.IsAssignableTo(typeof(ExpandoObject)))
        {
            CompareDictionaryValues((leftValue as IDictionary<string, object>)!, (rightValue as IDictionary<string, object>)!, propertyPath, mode, differences);
        }
        else if (type.IsAssignableTo(typeof(IOneOf)))
        {
            var valueProperty = type.GetProperty(nameof(IOneOf.Value));
            var leftOneOfValue = valueProperty?.GetValue(leftValue);
            var rightOneOfValue = valueProperty?.GetValue(rightValue);
            var oneOfValueType = leftOneOfValue?.GetType() ?? rightOneOfValue?.GetType();

            if (oneOfValueType is not null)
            {
                CompareValues(oneOfValueType, leftOneOfValue, rightOneOfValue, propertyPath, mode, differences);
            }
            else if (!Equals(leftOneOfValue, rightOneOfValue))
            {
                differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
            }
        }
        else if (type.IsDictionary())
        {
            var dictionaryDifferences = new List<PropertyDifference>();
            CompareDictionaryValues((leftValue as IEnumerable)!, (rightValue as IEnumerable)!, propertyPath, mode, dictionaryDifferences);
            if (dictionaryDifferences.Count > 0)
            {
                differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
            }
        }
        else if (!type.IsPrimitive &&
          type != typeof(Guid) &&
          type != typeof(string) &&
          !type.IsConcept() &&
          !type.IsEnumerable() &&
          !type.IsComparable())
        {
            ComparePropertiesFor(type, leftValue, rightValue, propertyPath, mode, differences);
        }
        else if (leftValue is not null && rightValue is not null && type.IsEnumerable())
        {
            CompareEnumerableValues(type, leftValue, rightValue, propertyPath, mode, differences);
        }
        else
        {
            var different = false;

            if (leftValue!.GetType() != rightValue!.GetType())
            {
                differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
                return;
            }

            if (type.IsComparable())
            {
                if (type.Implements(typeof(IComparable)))
                {
                    different = (leftValue as IComparable)!.CompareTo(rightValue) != 0;
                }
                else
                {
                    var comparableInterface = type.GetInterface(typeof(IComparable<>).Name);
                    var compareToMethod = comparableInterface!.GetMethod(nameof(IComparable<object>.CompareTo), BindingFlags.Public | BindingFlags.Instance);
                    different = ((int)compareToMethod!.Invoke(leftValue, [rightValue])!) != 0;
                }
            }
            else if (!leftValue!.Equals(rightValue))
            {
                different = true;
            }

            if (different)
            {
                differences.Add(new PropertyDifference(propertyPath, leftValue, rightValue));
            }
        }
    }
}
