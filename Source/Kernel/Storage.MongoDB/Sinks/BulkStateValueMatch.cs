// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Matches a value reached through a property path against the cached state of a bulk window.
/// </summary>
static class BulkStateValueMatch
{
    const string NullBucket = "\0null";

    /// <summary>
    /// Gets whether the document holds the target value at the path, descending through child collections and nested objects.
    /// </summary>
    /// <param name="document">The cached document.</param>
    /// <param name="pathSegments">The segments of the path.</param>
    /// <param name="segmentIndex">The segment to resolve next.</param>
    /// <param name="targetValue">The value to look for.</param>
    /// <returns>True if found, false if not.</returns>
    public static bool TryFindValueInDocument(ExpandoObject document, IPropertyPathSegment[] pathSegments, int segmentIndex, object targetValue)
    {
        if (segmentIndex >= pathSegments.Length)
        {
            return false;
        }

        var currentSegment = pathSegments[segmentIndex];
        var dict = (IDictionary<string, object?>)document;

        if (!dict.TryGetValue(currentSegment.Value, out var value) || value is null)
        {
            return false;
        }

        return segmentIndex == pathSegments.Length - 1
            ? ValuesAreEqual(value, targetValue)
            : TryFindValueInChildren(value, pathSegments, segmentIndex, targetValue);
    }

    /// <summary>
    /// Gets every value the path reaches in the document, using the same traversal as <see cref="TryFindValueInDocument"/>.
    /// </summary>
    /// <param name="document">The cached document.</param>
    /// <param name="pathSegments">The segments of the path.</param>
    /// <returns>The non-null values found at the end of the path.</returns>
    public static IEnumerable<object> ExtractLeafValues(ExpandoObject document, IPropertyPathSegment[] pathSegments) =>
        ExtractLeafValues(document, pathSegments, 0);

    /// <summary>
    /// Gets the bucket keys a value is indexed and probed under. Two values for which <see cref="ValuesAreEqual"/> holds always share a bucket key.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The bucket keys.</returns>
    public static IEnumerable<string> BucketKeys(object value)
    {
        yield return $"s:{value.ToString() ?? NullBucket}";

        // These types are equal under Equals while their ToString differ, so they need a normalized key of their own.
        switch (value)
        {
            case DateTimeOffset dateTimeOffset:
                yield return $"t:{dateTimeOffset.UtcTicks}";
                break;
            case decimal number:
                yield return $"d:{number.GetHashCode()}";
                break;
            case double number:
                yield return number == 0 ? "f:0" : $"f:{number.ToString("R", CultureInfo.InvariantCulture)}";
                break;
            case float number:
                yield return number == 0 ? "f:0" : $"f:{number.ToString("R", CultureInfo.InvariantCulture)}";
                break;
        }
    }

    /// <summary>
    /// Gets whether the two values are considered the same value.
    /// </summary>
    /// <param name="value">The cached value.</param>
    /// <param name="targetValue">The value to look for.</param>
    /// <returns>True if equal, false if not.</returns>
    public static bool ValuesAreEqual(object value, object targetValue)
    {
        if (value.Equals(targetValue))
        {
            return true;
        }

        return value.ToString() == targetValue.ToString();
    }

    static IEnumerable<object> ExtractLeafValues(ExpandoObject document, IPropertyPathSegment[] pathSegments, int segmentIndex)
    {
        if (segmentIndex >= pathSegments.Length)
        {
            yield break;
        }

        var dict = (IDictionary<string, object?>)document;
        if (!dict.TryGetValue(pathSegments[segmentIndex].Value, out var value) || value is null)
        {
            yield break;
        }

        if (segmentIndex == pathSegments.Length - 1)
        {
            yield return value;
            yield break;
        }

        switch (value)
        {
            case IEnumerable<object> collection:
                foreach (var leaf in collection.OfType<ExpandoObject>().SelectMany(item => ExtractLeafValues(item, pathSegments, segmentIndex + 1)))
                {
                    yield return leaf;
                }

                break;
            case ExpandoObject nested:
                foreach (var leaf in ExtractLeafValues(nested, pathSegments, segmentIndex + 1))
                {
                    yield return leaf;
                }

                break;
        }
    }

    static bool TryFindValueInChildren(object value, IPropertyPathSegment[] pathSegments, int segmentIndex, object targetValue) =>
        value switch
        {
            IEnumerable<object> collection => collection.OfType<ExpandoObject>().Any(item => TryFindValueInDocument(item, pathSegments, segmentIndex + 1, targetValue)),
            ExpandoObject nestedExpando => TryFindValueInDocument(nestedExpando, pathSegments, segmentIndex + 1, targetValue),
            _ => false
        };
}
