// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace Cratis.Chronicle.Testing.Assertions;

/// <summary>
/// Carries one comparison's mode and ancestor pairs, allowing cyclic graphs to be compared safely.
/// </summary>
/// <param name="mode">The contract for this comparison.</param>
internal sealed class ExpectedObjectComparison(ValueComparison mode)
{
    readonly List<(object Expected, object Actual)> _ancestors = [];

    /// <summary>
    /// Compares one pair while remembering only its active ancestor pairs.
    /// </summary>
    /// <param name="expected">The expected value or shape.</param>
    /// <param name="actual">The actual value.</param>
    /// <param name="path">The current member path.</param>
    /// <returns>The first mismatch, or null.</returns>
    internal ValueDifference? Compare(object? expected, object? actual, ValuePath path)
    {
        if (ReferenceEquals(expected, actual)) return null;
        if (expected is null || actual is null) return path.Difference(expected, actual);
        if (RequiresSameType(expected, actual) && expected.GetType() != actual.GetType())
        {
            return path.Member("Type").Difference(expected.GetType().FullName, actual.GetType().FullName);
        }
        if (ExpectedValueComparer.IsScalar(expected.GetType())) return CompareScalar(expected, actual, path);
        if (IsAncestorPair(expected, actual)) return null;

        return CompareGraph(expected, actual, path);
    }

    static ValueDifference? CompareScalar(object expected, object actual, ValuePath path) =>
        Equals(expected, actual) ? null : path.Difference(expected, actual);

    static PropertyInfo[] Properties(object value) => value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0)
        .OrderBy(property => property.Name, StringComparer.Ordinal).ToArray();

    static ValueDifference? FindExtraProperty(PropertyInfo[] expected, IReadOnlyDictionary<string, PropertyInfo> actual, object instance, ValuePath path)
    {
        var extra = actual.Keys.Except(expected.Select(property => property.Name), StringComparer.Ordinal).FirstOrDefault();

        return extra is null ? null : path.Member(extra).Difference("<missing>", actual[extra].GetValue(instance));
    }

    bool RequiresSameType(object expected, object actual) =>
        ExpectedValueComparer.IsScalar(expected.GetType()) || ExpectedValueComparer.IsScalar(actual.GetType()) ||
        RequiresTypedObject(expected, actual);

    bool RequiresTypedObject(object expected, object actual) => mode == ValueComparison.Typed &&
        (_ancestors.Count == 0 || expected is not IEnumerable || actual is not IEnumerable);

    bool IsAncestorPair(object expected, object actual) =>
        _ancestors.Exists(pair => ReferenceEquals(pair.Expected, expected) && ReferenceEquals(pair.Actual, actual));

    ValueDifference? CompareGraph(object expected, object actual, ValuePath path)
    {
        _ancestors.Add((expected, actual));
        try
        {
            return CompareContent(expected, actual, path);
        }
        finally
        {
            _ancestors.RemoveAt(_ancestors.Count - 1);
        }
    }

    ValueDifference? CompareContent(object expected, object actual, ValuePath path)
    {
        var expectedDictionary = ExpectedDictionary.Entries(expected);
        var actualDictionary = ExpectedDictionary.Entries(actual);
        if (expectedDictionary is not null || actualDictionary is not null)
        {
            return CompareDictionaries(expectedDictionary, actualDictionary, path);
        }
        if (expected is IEnumerable || actual is IEnumerable)
        {
            return CompareCollections(expected, actual, path);
        }

        return CompareProperties(expected, actual, path);
    }

    ValueDifference? CompareCollections(object expected, object actual, ValuePath path) =>
        expected is IEnumerable expectedItems && actual is IEnumerable actualItems
            ? CompareItems(expectedItems.Cast<object?>().ToArray(), actualItems.Cast<object?>().ToArray(), path)
            : path.Difference(expected, actual);

    ValueDifference? CompareProperties(object expected, object actual, ValuePath path)
    {
        var expectedProperties = Properties(expected);
        var actualProperties = Properties(actual).ToDictionary(property => property.Name, StringComparer.Ordinal);
        foreach (var property in expectedProperties)
        {
            var memberPath = path.Member(property.Name);
            if (!actualProperties.TryGetValue(property.Name, out var actualProperty)) return memberPath.Difference(property.GetValue(expected), "<missing>");
            var difference = Compare(property.GetValue(expected), actualProperty.GetValue(actual), memberPath);
            if (difference is not null) return difference;
        }

        if (expectedProperties.Length == 0 && actualProperties.Count == 0) return CompareScalar(expected, actual, path);

        return mode == ValueComparison.Partial ? null : FindExtraProperty(expectedProperties, actualProperties, actual, path);
    }

    ValueDifference? CompareItems(object?[] expected, object?[] actual, ValuePath path)
    {
        for (var index = 0; index < Math.Max(expected.Length, actual.Length); index++)
        {
            var itemPath = path.Element(index);
            if (index >= expected.Length) return itemPath.Difference("<missing>", actual[index]);
            if (index >= actual.Length) return itemPath.Difference(expected[index], "<missing>");
            var difference = Compare(expected[index], actual[index], itemPath);
            if (difference is not null) return difference;
        }

        return null;
    }

    ValueDifference? CompareDictionaries(IReadOnlyList<DictionaryEntry>? expected, IReadOnlyList<DictionaryEntry>? actual, ValuePath path)
    {
        if (expected is null || actual is null) return path.Difference("dictionary", "different collection kind");
        foreach (var entry in expected)
        {
            var matching = actual.Where(candidate => Equals(candidate.Key, entry.Key)).ToArray();
            var itemPath = path.Element(ExpectedValueComparer.Describe(entry.Key));
            if (matching.Length == 0) return itemPath.Difference(entry.Value, "<missing>");
            var difference = Compare(entry.Value, matching[0].Value, itemPath);
            if (difference is not null) return difference;
        }

        return expected.Count == actual.Count ? null : path.Member("Count").Difference(expected.Count, actual.Count);
    }
}
