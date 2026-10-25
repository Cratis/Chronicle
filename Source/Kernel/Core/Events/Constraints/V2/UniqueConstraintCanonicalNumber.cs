// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Canonicalizes schema-representable numeric values without culture-dependent text.
/// </summary>
internal static class UniqueConstraintCanonicalNumber
{
    internal static string Single(float value)
    {
        if (!float.IsFinite(value))
        {
            throw new NonFiniteUniqueConstraintNumber();
        }
        return value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
    }

    internal static string Double(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new NonFiniteUniqueConstraintNumber();
        }
        return value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
    }

    internal static string Decimal(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);

    internal static string Integer(object value) => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
}
