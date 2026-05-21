// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services;

/// <summary>
/// Provides compatibility helpers for TypeScript contracts generated with <c>forceLong=number</c>.
/// </summary>
internal static class TypeScriptSequenceNumberCompatibility
{
    /// <summary>
    /// The largest integer value TypeScript can safely represent as a number.
    /// </summary>
    public const ulong MaxSafeInteger = 9_007_199_254_740_991UL;

    /// <summary>
    /// Converts a sequence number sentinel value to a TypeScript-safe wire value.
    /// </summary>
    /// <param name="value">Value to sanitize.</param>
    /// <returns>Sanitized value.</returns>
    public static ulong SanitizeForWire(ulong value) =>
        value == ulong.MaxValue ? MaxSafeInteger : value;
}
