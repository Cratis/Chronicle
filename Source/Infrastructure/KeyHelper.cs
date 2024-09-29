// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public static T Parse<T>(string key)
    {
        var elements = key.Split(Separator);
        var constructor = typeof(T).GetConstructors()[0];
        var parameters = constructor.GetParameters();
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
}
