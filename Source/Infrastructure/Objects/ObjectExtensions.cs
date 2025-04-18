// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Concepts;
using Cratis.Json;
using Cratis.Reflection;
using Cratis.Strings;

namespace Cratis.Chronicle.Objects;

/// <summary>
/// Extension methods for any object.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Deep clone an object.
    /// </summary>
    /// <param name="source">Object clone.</param>
    /// <typeparam name="T">Type of object to clone.</typeparam>
    /// <returns>Cloned instance.</returns>
    public static T Clone<T>(this T source)
    {
        if (source is null) return default!;

        if (source is ExpandoObject expandoObject)
        {
            return (T)(object)ExpandoObjectExtensions.Clone(expandoObject);
        }

        if (source is Guid)
        {
            return source;
        }

        var valueType = source.GetType();
        if (valueType.IsPrimitive || valueType == typeof(string)) return source;
        if (valueType.IsConcept())
        {
            return (T)ConceptFactory.CreateConceptInstance(valueType, source.GetConceptValue());
        }

        var sourceAsString = JsonSerializer.Serialize(source, Globals.JsonSerializerOptions);
        return (T)JsonSerializer.Deserialize(sourceAsString, valueType, Globals.JsonSerializerOptions)!;
    }

    /// <summary>
    /// Ensure a <see cref="PropertyPath"/> on an object.
    /// </summary>
    /// <param name="source">Object to ensure on.</param>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to ensure.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    /// <returns>Instance of the last segment in the path.</returns>
    /// <exception cref="UnableToResolvePropertyPathOnType">Thrown if not able to resolve parts of the property path on the type.</exception>
    public static object EnsurePath(this object source, PropertyPath propertyPath, ArrayIndexers arrayIndexers)
    {
        var currentType = source.GetType();
        var currentInstance = source;
        var parentInstance = source;
        var currentPath = new PropertyPath(string.Empty);

        var segments = propertyPath.Segments.ToArray();
        for (var segmentIndex = 0; segmentIndex < segments.Length - 1; segmentIndex++)
        {
            var segment = segments[segmentIndex];
            currentPath += segment;
            parentInstance = currentInstance;

            var currentPropertyInfo = currentType.GetProperties().SingleOrDefault(_ => _.Name == segment.Value || _.Name == segment.Value.ToPascalCase())
                ?? throw new UnableToResolvePropertyPathOnType(currentType, propertyPath);
            currentInstance = currentPropertyInfo.GetValue(currentInstance);
            if (currentInstance is null)
            {
                var newInstance = CreateInstanceOf(currentPropertyInfo.PropertyType);
                currentPropertyInfo.SetValue(parentInstance, newInstance);
                currentInstance = newInstance;
            }
            currentType = currentPropertyInfo.PropertyType;

            switch (segment)
            {
                case ArrayProperty arrayProperty:
                    {
                        var indexer = arrayIndexers.GetFor(currentPath);
                        var element = (currentInstance as IEnumerable)!
                            .Cast<object>()
                            .SingleOrDefault(_ =>
                            {
                                var property = _.GetType().GetProperties().SingleOrDefault(_ => _.Name == segment.Value || _.Name == segment.Value.ToPascalCase());
                                return property is not null && (property.GetValue(_)?.Equals(indexer.Identifier) ?? false);
                            });

                        if (element is null)
                        {
                            var elementType = typeof(object);
                            var collectionType = currentInstance.GetType();
                            if (collectionType.IsGenericType)
                            {
                                elementType = collectionType.GetGenericArguments()[0];
                            }
                            element = CreateInstanceOf(elementType)!;
                        }

                        if (currentInstance is IList collection)
                        {
                            collection.Add(element);
                        }
                        currentInstance = element;
                        currentType = element.GetType();
                    }
                    break;
            }
        }

        return currentInstance!;
    }

    /// <summary>
    /// Check for equality between two objects.
    /// </summary>
    /// <param name="left">Left object to compare with.</param>
    /// <param name="right">Right object to compare with.</param>
    /// <returns>True if the two are equal, false if not.</returns>
    /// <remarks>
    /// This method recognizes <see cref="ExpandoObject"/> and will compare the key/value pairs.
    /// If its not an <see cref="ExpandoObject"/>, it will use the <see cref="object.Equals(object)"/> method.
    /// </remarks>
    public static bool IsEqualTo(this object left, object right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;

        if (left is ExpandoObject leftAsExpandoObject && right is ExpandoObject rightAsExpandoObject)
        {
            var leftKeyValuePairs = leftAsExpandoObject.GetKeyValuePairs().ToDictionary(_ => _.Key, _ => _.Value);
            var rightKeyValuePairs = rightAsExpandoObject.GetKeyValuePairs().ToDictionary(_ => _.Key, _ => _.Value);

            foreach (var (key, value) in leftKeyValuePairs)
            {
                if (!rightKeyValuePairs.TryGetValue(key, out var rightValuePairValue)) return false;
                if (!IsEqualTo(value, rightValuePairValue)) return false;
            }

            return true;
        }

        return left.Equals(right);
    }

    static object CreateInstanceOf(Type type)
    {
        if (type == typeof(string))
        {
            return string.Empty;
        }
        if (type == typeof(Guid))
        {
            return Guid.Empty;
        }
        if (!type.IsPrimitive)
        {
            var arguments = new List<object>();
            if (type.IsAssignableTo(typeof(IEnumerable)) && type != typeof(string))
            {
                if (type.IsGenericType)
                {
                    var list = typeof(List<>).MakeGenericType(type.GetGenericArguments());
                    return Activator.CreateInstance(list)!;
                }
                return Array.Empty<object>();
            }

            var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();
            if (constructor is not null)
            {
                arguments.AddRange(constructor.GetParameters().Select(_ => CreateInstanceOf(_.ParameterType)));
                return constructor.Invoke([.. arguments]);
            }
        }

        return Activator.CreateInstance(type)!;
    }
}
