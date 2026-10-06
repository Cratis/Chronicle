// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Primitives;

/// <summary>
/// Conversions for presence-aware boolean contract values.
/// </summary>
public static class BooleanValueExtensions
{
    /// <summary>
    /// Converts a boolean to an explicit wire value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>The explicit wire value.</returns>
    public static BooleanValue ToBooleanValue(this bool value) => value ? BooleanValue.True : BooleanValue.False;

    /// <summary>
    /// Resolves an explicit value, falling back to the legacy field for older clients.
    /// </summary>
    /// <param name="value">The explicit wire value.</param>
    /// <param name="legacyValue">The legacy boolean field.</param>
    /// <returns>The resolved boolean value.</returns>
    public static bool Resolve(this BooleanValue value, bool legacyValue) => value switch
    {
        BooleanValue.True => true,
        BooleanValue.False => false,
        _ => legacyValue
    };
}
