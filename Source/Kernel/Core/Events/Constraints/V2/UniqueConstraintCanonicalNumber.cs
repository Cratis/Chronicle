// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Canonicalizes schema-representable numeric values without culture-dependent text.
/// </summary>
internal static class UniqueConstraintCanonicalNumber
{
    /// <summary>
    /// Canonicalizes a single-precision number as round-trip invariant text, with zero normalized to "0".
    /// </summary>
    /// <param name="value">The number to canonicalize.</param>
    /// <returns>The canonical text.</returns>
    /// <exception cref="NonFiniteUniqueConstraintNumber">The number is NaN or infinity.</exception>
    internal static string Single(float value)
    {
        if (!float.IsFinite(value))
        {
            throw new NonFiniteUniqueConstraintNumber();
        }
        return value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Canonicalizes a double-precision number as round-trip invariant text, with zero normalized to "0".
    /// </summary>
    /// <param name="value">The number to canonicalize.</param>
    /// <returns>The canonical text.</returns>
    /// <exception cref="NonFiniteUniqueConstraintNumber">The number is NaN or infinity.</exception>
    internal static string Double(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new NonFiniteUniqueConstraintNumber();
        }
        return value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Canonicalizes a decimal as invariant text with up to 28 fractional digits and no trailing zeros.
    /// </summary>
    /// <param name="value">The decimal to canonicalize.</param>
    /// <returns>The canonical text.</returns>
    internal static string Decimal(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);

    /// <summary>
    /// Canonicalizes a boxed integral value as invariant text.
    /// </summary>
    /// <param name="value">The boxed integer, which must be formattable.</param>
    /// <returns>The canonical text.</returns>
    internal static string Integer(object value) => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
}
