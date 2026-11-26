// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Concepts;

namespace Cratis.Chronicle.Testing.Assertions;

/// <summary>
/// Compares public object shapes without involving serialization contracts.
/// </summary>
internal static class ExpectedValueComparer
{
    static readonly HashSet<Type> _scalarTypes =
    [
        typeof(string), typeof(decimal), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset),
        typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan), typeof(Uri)
    ];

    /// <summary>
    /// Compares two values using an explicit comparison contract.
    /// </summary>
    /// <param name="expected">The expected value or shape.</param>
    /// <param name="actual">The actual value.</param>
    /// <param name="mode">The comparison contract.</param>
    /// <returns>The first difference, or null when matching.</returns>
    internal static ValueDifference? Compare(object? expected, object? actual, ValueComparison mode) =>
        new ExpectedObjectComparison(mode).Compare(expected, actual, new("$"));

    /// <summary>
    /// Determines whether a type has scalar value semantics.
    /// </summary>
    /// <param name="type">The runtime type.</param>
    /// <returns>Whether to compare by typed value equality.</returns>
    internal static bool IsScalar(Type type) => type.IsPrimitive || type.IsEnum || type.IsConcept() || _scalarTypes.Contains(type);

    /// <summary>
    /// Describes one value without serializing an arbitrary graph.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A concise value or type description.</returns>
    internal static string Describe(object? value) => value switch
    {
        null => "<null>",
        _ when IsScalar(value.GetType()) => $"{value} ({value.GetType().Name})",
        _ => $"<{value.GetType().Name}>"
    };

    /// <summary>
    /// Describes an unmatched payload's public members without traversing its graph.
    /// </summary>
    /// <param name="value">The payload.</param>
    /// <returns>The runtime type and shallow member values.</returns>
    internal static string DescribePayload(object? value)
    {
        if (value is null || IsScalar(value.GetType())) return Describe(value);
        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => $"{property.Name}={Describe(property.GetValue(value))}");

        return $"{value.GetType().Name} {{ {string.Join(", ", properties)} }}";
    }
}
