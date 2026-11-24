// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Keeps the state of a read model identified by a <see cref="FlatCompositeKey"/> in agreement with its key.
/// </summary>
/// <remarks>
/// The parts of a flat key are ordinary root properties of the read model, so the instance has to hold them and nothing
/// a projection does may make them disagree with the key.
/// </remarks>
public static class FlatCompositeKeyState
{
    /// <summary>
    /// Gets the text identifying a key, for places that need a string: the encoding of the parts of a <see cref="FlatCompositeKey"/>, otherwise the key value.
    /// </summary>
    /// <param name="key">The <see cref="Key"/>.</param>
    /// <returns>The text identifying the key.</returns>
    public static string Text(Key key) => key is FlatCompositeKey flat
        ? CompositeKeyEncoding.Encode(flat.Parts)
        : key.Value.ToString()!;

    /// <summary>
    /// Makes the state hold every part of the key, recording the parts it was missing as changes.
    /// </summary>
    /// <param name="changeset">The <see cref="IChangeset{TSource, TTarget}"/> the changes are recorded in.</param>
    /// <param name="state">The state to seed.</param>
    /// <param name="key">The <see cref="FlatCompositeKey"/> identifying the read model.</param>
    /// <exception cref="FlatCompositeKeyIdentityViolated">The state holds a part with another value than the key.</exception>
    public static void Seed(IChangeset<AppendedEvent, ExpandoObject> changeset, ExpandoObject state, FlatCompositeKey key)
    {
        var values = (IDictionary<string, object?>)state;
        var missing = new List<PropertyDifference>();
        foreach (var (part, value) in key.Parts)
        {
            if (values.TryGetValue(part, out var existing) && existing is not null)
            {
                ThrowIfDifferent(part, value, existing);
                continue;
            }

            values[part] = value;
            missing.Add(new PropertyDifference(new PropertyPath(part), null, value));
        }

        if (missing.Count > 0)
        {
            changeset.Add(new PropertiesChanged<ExpandoObject>(changeset.CurrentState, missing));
        }
    }

    /// <summary>
    /// Verifies that the state the changeset results in still carries the parts of the key.
    /// </summary>
    /// <param name="changeset">The <see cref="IChangeset{TSource, TTarget}"/> to verify.</param>
    /// <param name="key">The <see cref="FlatCompositeKey"/> identifying the read model.</param>
    /// <exception cref="FlatCompositeKeyIdentityViolated">The state contradicts the key.</exception>
    /// <remarks>
    /// An instance that is removed has no identity to keep, so a removal is not verified.
    /// </remarks>
    public static void Verify(IChangeset<AppendedEvent, ExpandoObject> changeset, FlatCompositeKey key)
    {
        if (changeset.Changes.Any(_ => _ is Removed))
        {
            return;
        }

        var values = (IDictionary<string, object?>)changeset.CurrentState;
        foreach (var (part, value) in key.Parts)
        {
            values.TryGetValue(part, out var actual);
            ThrowIfDifferent(part, value, actual);
        }
    }

    static void ThrowIfDifferent(string part, object? expected, object? actual)
    {
        if (!AreEqual(expected, actual))
        {
            throw new FlatCompositeKeyIdentityViolated(part, expected, actual);
        }
    }

    static bool AreEqual(object? expected, object? actual)
    {
        if (expected is null || actual is null) return expected is null && actual is null;
        if (expected.Equals(actual)) return true;
        if (IsNumber(expected) && IsNumber(actual))
        {
            return Convert.ToDecimal(expected, CultureInfo.InvariantCulture) == Convert.ToDecimal(actual, CultureInfo.InvariantCulture);
        }

        return string.Equals(
            Convert.ToString(expected, CultureInfo.InvariantCulture),
            Convert.ToString(actual, CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    static bool IsNumber(object value) => value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
}
