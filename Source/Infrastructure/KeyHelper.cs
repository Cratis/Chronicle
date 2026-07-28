// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.Concepts;

namespace Cratis.Chronicle;

/// <summary>
/// Represents a key helper for combining and parsing keys.
/// </summary>
public static class KeyHelper
{
    /// <summary>
    /// Gets the separator character used in string representations.
    /// </summary>
    public const char Separator = '#';

    static readonly ConcurrentDictionary<Type, (ConstructorInfo Constructor, ParameterInfo[] Parameters)> _constructorsByType = new();

    /// <summary>
    /// Combine the parts into a string representation of a key.
    /// </summary>
    /// <param name="parts">Parts to combine.</param>
    /// <returns>The combined string.</returns>
    public static string Combine(params object[] parts)
    {
        parts = parts.Where(_ => _ is not null).ToArray();
        return string.Join(Separator, parts);
    }

    /// <summary>
    /// Create a key from the parts.
    /// </summary>
    /// <param name="key">String representation to create from.</param>
    /// <typeparam name="T">Type of key to create.</typeparam>
    /// <returns>A new instance of the key.</returns>
    /// <remarks>
    /// The reflected constructor and parameters for <typeparamref name="T"/> are memoized. The set of grain key
    /// types is fixed at compile time, so the cache is bounded and never needs invalidation.
    /// </remarks>
    public static T Parse<T>(string key)
    {
        var elements = key.Split(Separator);
        var (constructor, parameters) = _constructorsByType.GetOrAdd(typeof(T), ResolveConstructorSignature);
        List<object> arguments = [];

        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
        {
            if (parameterIndex >= elements.Length)
            {
                arguments.Add(null!);
                continue;
            }

            var element = elements[parameterIndex];
            object argument = string.IsNullOrEmpty(element) ? null! : element;

            if (argument is not null)
            {
                if (parameters[parameterIndex].ParameterType.IsConcept())
                {
                    argument = ConceptFactory.CreateConceptInstance(parameters[parameterIndex].ParameterType, argument);
                }
                else if (parameters[parameterIndex].ParameterType != typeof(string))
                {
                    argument = Convert.ChangeType(elements[parameterIndex], parameters[parameterIndex].ParameterType);
                }
            }

            arguments.Add(argument!);
        }

        return (T)constructor.Invoke([.. arguments]);
    }

    static (ConstructorInfo Constructor, ParameterInfo[] Parameters) ResolveConstructorSignature(Type type)
    {
        var constructor = type.GetConstructors()[0];
        return (constructor, constructor.GetParameters());
    }
}
