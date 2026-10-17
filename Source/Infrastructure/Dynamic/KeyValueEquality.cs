// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Types;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Compares materialized projection keys by value, including composite keys and scalar type conversions.
/// </summary>
public static class KeyValueEquality
{
    /// <summary>
    /// Compares a stored key with a lookup key without requiring either key to implement <see cref="IConvertible"/>.
    /// </summary>
    /// <param name="storedKey">The stored key, whose scalar type takes precedence during conversion.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>Whether the keys represent the same value.</returns>
    public static bool AreEqual(object? storedKey, object? key)
    {
        if (Equals(storedKey, key)) return true;
        if (storedKey is null || key is null) return false;

        if (storedKey.GetType() != key.GetType())
        {
            try
            {
                if (storedKey.Equals(TypeConversion.Convert(storedKey.GetType(), key))) return true;
            }
            catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException or ArgumentException)
            {
                // Composite and incompatible scalar keys are compared by their materialized values below.
            }
        }

        try
        {
            // SQL can materialize a composite key as JSON text; other stores use an object.
            var storedValue = AsJsonValue(storedKey);
            var lookupValue = AsJsonValue(key);
            return JsonElement.DeepEquals(storedValue, lookupValue);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // A key without a serializable representation only matches through value equality above.
            return false;
        }
    }

    static JsonElement AsJsonValue(object value) => value is string text && text.StartsWith('{')
        ? JsonSerializer.Deserialize<JsonElement>(text)
        : JsonSerializer.SerializeToElement(value);
}
