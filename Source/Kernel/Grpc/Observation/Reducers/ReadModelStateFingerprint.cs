// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;

namespace Cratis.Chronicle.Services.Observation.Reducers;

/// <summary>
/// Computes a 64-bit fingerprint of a read model state without serializing it.
/// </summary>
/// <remarks>
/// Two states with the same content get the same fingerprint no matter the order of dictionary entries and no matter
/// which numeric type carries an integral value. Lists are ordered. Dates, date-times, guids and enums are fingerprinted by
/// their invariant string. A difference in content gives a different fingerprint, bar a 64-bit collision. Two states with
/// the same content but represented with different types (for instance a double against a decimal holding a fraction)
/// may get different fingerprints; that only ever causes a full state send, never a false "unchanged".
/// </remarks>
internal static class ReadModelStateFingerprint
{
    const int MaxDepth = 256;
    const ulong Multiplier = 0x9E3779B97F4A7C15UL;

    const ulong NullTag = 0x01;
    const ulong BooleanTag = 0x02;
    const ulong StringTag = 0x03;
    const ulong NumberTag = 0x04;
    const ulong FloatTag = 0x05;
    const ulong DecimalTag = 0x06;
    const ulong ObjectTag = 0x07;
    const ulong ListTag = 0x08;
    const ulong UnsignedTag = 0x09;

    /// <summary>
    /// Try to compute the fingerprint of a value.
    /// </summary>
    /// <param name="value">The state, or any value within it.</param>
    /// <param name="fingerprint">The computed fingerprint.</param>
    /// <returns>True if a fingerprint could be computed; false if the value nests too deeply to be fingerprinted.</returns>
    internal static bool TryCompute(object? value, out ulong fingerprint)
    {
        var ok = true;
        fingerprint = Compute(value, 0, ref ok);
        return ok;
    }

    static ulong Compute(object? value, int depth, ref bool ok)
    {
        if (depth > MaxDepth)
        {
            ok = false;
            return 0;
        }

        switch (value)
        {
            case null:
                return Mix(NullTag);

            case string text:
                return HashString(text);

            case bool boolean:
                return Mix(Tagged(BooleanTag, boolean ? 1UL : 0UL));

            case sbyte or short or int or long or byte or ushort or uint:
                return HashSigned(Convert.ToInt64(value, CultureInfo.InvariantCulture));

            case ulong unsigned:
                return unsigned <= long.MaxValue ? HashSigned((long)unsigned) : Mix(Tagged(UnsignedTag, unsigned));

            case float single:
                return HashDouble(single);

            case double number:
                return HashDouble(number);

            case decimal number:
                return HashDecimal(number);

            case DateTime dateTime:
                return HashString(dateTime.ToString("O", CultureInfo.InvariantCulture));

            case DateTimeOffset dateTimeOffset:
                return HashString(dateTimeOffset.ToString("O", CultureInfo.InvariantCulture));

            case Guid guid:
                return HashString(guid.ToString("D", CultureInfo.InvariantCulture));

            case Enum enumValue:
                return HashString(enumValue.ToString());

            case IDictionary<string, object?> dictionary:
                return HashDictionary(dictionary, depth, ref ok);

            case IEnumerable enumerable:
                return HashList(enumerable, depth, ref ok);

            default:
                return HashString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    static ulong HashDictionary(IDictionary<string, object?> dictionary, int depth, ref bool ok)
    {
        // Entries are combined with a commutative operation so the order of the entries does not matter.
        var sum = 0UL;
        foreach (var (key, entry) in dictionary)
        {
            var entryHash = Mix(Tagged(ObjectTag, HashString(key)) ^ Compute(entry, depth + 1, ref ok));
            unchecked
            {
                sum += entryHash;
            }
        }

        return Mix(Tagged(ObjectTag, unchecked(sum + ((ulong)dictionary.Count * Multiplier))));
    }

    static ulong HashList(IEnumerable enumerable, int depth, ref bool ok)
    {
        var hash = Tagged(ListTag, 0);
        var count = 0UL;
        foreach (var item in enumerable)
        {
            hash = Mix(hash ^ Compute(item, depth + 1, ref ok));
            count++;
        }

        return Mix(unchecked(hash + count));
    }

    static ulong HashString(string text)
    {
        var hash = Tagged(StringTag, 0);
        foreach (var character in text)
        {
            unchecked
            {
                hash = (hash ^ character) * Multiplier;
            }

            hash ^= hash >> 32;
        }

        return Mix(unchecked(hash + (ulong)text.Length));
    }

    static ulong HashSigned(long value) => Mix(Tagged(NumberTag, unchecked((ulong)value)));

    static ulong HashDouble(double value)
    {
        if (value == Math.Truncate(value) && Math.Abs(value) < 9.0e18)
        {
            return HashSigned((long)value);
        }

        // Negative zero is integral and handled above; NaNs are all normalized to one pattern.
        var bits = double.IsNaN(value) ? 0x7FF8000000000000UL : BitConverter.DoubleToUInt64Bits(value);
        return Mix(Tagged(FloatTag, bits));
    }

    static ulong HashDecimal(decimal value)
    {
        if (value == decimal.Truncate(value) && Math.Abs(value) < 9.0e18m)
        {
            return HashSigned((long)value);
        }

        // Dividing by 1.0000...m strips trailing zeros so that 1.50m and 1.5m agree.
        var normalized = value / 1.0000000000000000000000000000m;
        var bits = decimal.GetBits(normalized);
        var hash = Tagged(DecimalTag, 0);
        foreach (var part in bits)
        {
            hash = Mix(hash ^ unchecked((uint)part));
        }

        return hash;
    }

    static ulong Tagged(ulong tag, ulong value) => unchecked((tag * Multiplier) + value);

    /// <summary>
    /// The SplitMix64 finalizer.
    /// </summary>
    /// <param name="value">The value to mix.</param>
    /// <returns>The mixed value.</returns>
    static ulong Mix(ulong value)
    {
        unchecked
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
