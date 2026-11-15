// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Concepts;
using Cratis.Types;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Converts decimal values without an intermediate binary floating-point conversion.
/// </summary>
public static class DecimalValues
{
    const int MaxScale = 28;
    static readonly double[] _powers10 = [.. Enumerable.Range(0, 30).Select(power => double.Parse($"1e{power}", CultureInfo.InvariantCulture))];

    /// <summary>
    /// Converts a legacy double using its shortest round-trip representation.
    /// </summary>
    /// <param name="value">The stored double.</param>
    /// <returns>The decimal represented by the round-trip text.</returns>
    public static decimal FromDouble(double value) => decimal.Parse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts a float using its shortest round-trip representation.
    /// </summary>
    /// <param name="value">The stored float.</param>
    /// <returns>The decimal represented by the round-trip text.</returns>
    public static decimal FromSingle(float value) => decimal.Parse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts a double to a decimal the way .NET 10 and earlier did, by rounding to 15 significant digits, on every runtime.
    /// </summary>
    /// <param name="value">The double to convert.</param>
    /// <returns>The decimal rounded to 15 significant digits.</returns>
    /// <exception cref="OverflowException">The value is NaN, infinite or outside the decimal range.</exception>
    /// <remarks>
    /// .NET 11 converts a double to its exact decimal expansion (1.23d becomes 1.229999999999999982236431606m). This keeps the earlier result.
    /// </remarks>
    public static decimal FromDoubleRounded(double value) => FromRounded(value, 15);

    /// <summary>
    /// Converts a float to a decimal the way .NET 10 and earlier did, by rounding to 7 significant digits, on every runtime.
    /// </summary>
    /// <param name="value">The float to convert.</param>
    /// <returns>The decimal rounded to 7 significant digits.</returns>
    /// <exception cref="OverflowException">The value is NaN, infinite or outside the decimal range.</exception>
    public static decimal FromSingleRounded(float value) => FromRounded(value, 7);

    /// <summary>
    /// Converts a value to a decimal, using the .NET 10 rounding for double and float and <see cref="Convert.ToDecimal(object?, IFormatProvider?)"/> for everything else.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The decimal value.</returns>
    /// <exception cref="OverflowException">A double or float is NaN, infinite or outside the decimal range.</exception>
    public static decimal ToDecimal(object? value) => value switch
    {
        double doubleValue => FromDoubleRounded(doubleValue),
        float singleValue => FromSingleRounded(singleValue),
        _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Converts a value to a target type with <see cref="TypeConversion.Convert(Type, object)"/>, keeping the .NET 10 rounding when a double or float is converted to a decimal.
    /// </summary>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public static object ConvertTo(Type targetType, object value)
    {
        if ((Nullable.GetUnderlyingType(targetType) ?? targetType) != typeof(decimal))
        {
            return TypeConversion.Convert(targetType, value);
        }

        var unwrapped = value?.IsConcept() == true ? value.GetConceptValue() : value;
        return unwrapped is double or float ? ToDecimal(unwrapped) : TypeConversion.Convert(targetType, value!);
    }

    /// <summary>
    /// Reads a decimal from a JSON number or its string representation.
    /// </summary>
    /// <param name="value">The JSON value.</param>
    /// <param name="result">The decimal value.</param>
    /// <returns>Whether the value represents a decimal.</returns>
    public static bool TryFromLiteral(JsonValue value, out decimal result)
    {
        if (value.TryGetValue<decimal>(out result))
        {
            return true;
        }
        if (value.TryGetValue<double>(out var doubleValue))
        {
            return decimal.TryParse(doubleValue.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
        if (value.TryGetValue<float>(out var singleValue))
        {
            return decimal.TryParse(singleValue.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
        return decimal.TryParse(value.TryGetValue<string>(out var text) ? text : value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Port of the .NET 10 DecCalc.VarDecFromR8 and VarDecFromR4 algorithms.
    /// </summary>
    /// <param name="input">The value to convert.</param>
    /// <param name="digits">The number of significant digits, 15 for double and 7 for float.</param>
    /// <returns>The decimal.</returns>
    static decimal FromRounded(double input, int digits)
    {
        var exponent = (int)((BitConverter.DoubleToUInt64Bits(input) >> 52) & 0x7FF) - 1022;
        return Build(input, exponent, digits);
    }

    static decimal FromRounded(float input, int digits)
    {
        var exponent = (int)((BitConverter.SingleToUInt32Bits(input) >> 23) & 0xFF) - 126;
        return Build(input, exponent, digits);
    }

    static decimal Build(double input, int exponent, int digits)
    {
        if (exponent < -94)
        {
            return 0m;
        }

        if (exponent > 96)
        {
            throw new OverflowException();
        }

        var negative = input < 0;
        var (mantissa, power) = ScaleToMantissa(negative ? -input : input, digits - 1 - ((exponent * 19728) >> 16), digits);
        if (mantissa == 0)
        {
            return 0m;
        }

        return power < 0 ? Widen(mantissa, -power, negative) : Narrow(mantissa, power, digits, negative);
    }

    static (ulong Mantissa, int Power) ScaleToMantissa(double value, int power, int digits)
    {
        if (power >= 0)
        {
            power = Math.Min(power, MaxScale);
            value *= _powers10[power];
        }
        else if (power != -1 || value >= _powers10[digits])
        {
            value /= _powers10[-power];
        }
        else
        {
            power = 0;
        }

        if (value < _powers10[digits - 1] && power < MaxScale)
        {
            value *= 10;
            power++;
        }

        return ((ulong)(long)Math.Round(value), power);
    }

    static decimal Widen(ulong mantissa, int power, bool negative)
    {
        var product = (UInt128)mantissa * (UInt128)_powers10[power];
        return product > ((UInt128)uint.MaxValue << 64 | ulong.MaxValue)
            ? throw new OverflowException()
            : new decimal((int)(uint)product, (int)(uint)(product >> 32), (int)(uint)(product >> 64), negative, 0);
    }

    static decimal Narrow(ulong mantissa, int power, int digits, bool negative)
    {
        var removable = Math.Min(power, digits - 1);
        while (removable > 0 && mantissa % 10 == 0)
        {
            mantissa /= 10;
            power--;
            removable--;
        }

        return new decimal((int)(uint)mantissa, (int)(uint)(mantissa >> 32), 0, negative, (byte)power);
    }
}
