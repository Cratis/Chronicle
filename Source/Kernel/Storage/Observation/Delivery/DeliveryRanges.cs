// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Defines checked, provider-neutral delivery ranges. Writes reject invalid values before I/O; reads fail closed.
/// </summary>
public static class DeliveryRanges
{
    /// <summary>
    /// The inclusive maximum counter and position.
    /// </summary>
    public const long MaximumCounter = long.MaxValue;

    /// <summary>
    /// The inclusive maximum UTC tick value.
    /// </summary>
    public const long MaximumClock = 3155378975999999999;

    /// <summary>
    /// The inclusive maximum envelope size in bytes.
    /// </summary>
    public const int MaximumPayload = 65536;

    /// <summary>
    /// The inclusive maximum lane count.
    /// </summary>
    public const int MaximumLanes = 64;

    /// <summary>
    /// Converts a counter or position to its signed storage representation without wrapping.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="field">The field name.</param>
    /// <returns>The signed storage value.</returns>
    /// <exception cref="DeliveryValueOutOfRange">The counter exceeds the storage range.</exception>
    public static long Counter(ulong value, string field)
    {
        if (value > MaximumCounter)
        {
            throw new DeliveryValueOutOfRange(field);
        }

        return checked((long)value);
    }

    /// <summary>
    /// Converts an actual position to its signed storage representation, rejecting Unavailable.
    /// </summary>
    /// <param name="value">The position.</param>
    /// <param name="field">The field name.</param>
    /// <returns>The signed position.</returns>
    public static long Position(EventSequenceNumber value, string field) => Counter(value.Value, field);

    /// <summary>
    /// Validates a UTC tick clock before storage.
    /// </summary>
    /// <param name="value">The clock.</param>
    /// <param name="field">The field name.</param>
    /// <returns>The validated clock.</returns>
    /// <exception cref="DeliveryValueOutOfRange">The clock is outside the UTC tick range.</exception>
    public static long Clock(long value, string field)
    {
        if (value is < 0 or > MaximumClock)
        {
            throw new DeliveryValueOutOfRange(field);
        }

        return value;
    }

    /// <summary>
    /// Validates an Int32 field before storage, including format, versions, attempts, slices and lengths.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="field">The field name.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="DeliveryValueOutOfRange">The value is outside its range.</exception>
    public static int Bounded(int value, string field, int minimum = 0, int maximum = int.MaxValue)
    {
        if (value < minimum || value > maximum)
        {
            throw new DeliveryValueOutOfRange(field);
        }

        return value;
    }

    /// <summary>
    /// Validates a half-open slice interval.
    /// </summary>
    /// <param name="from">The inclusive start.</param>
    /// <param name="to">The exclusive end.</param>
    /// <exception cref="DeliveryValueOutOfRange">The interval is empty, inverted, or outside the slice space.</exception>
    public static void Slices(ushort from, ushort to)
    {
        if (from >= to || to > SequencingKeySlice.Count)
        {
            throw new DeliveryValueOutOfRange(nameof(SliceRange));
        }
    }

    /// <summary>
    /// Restores a counter or position from Int64 storage after checking its range.
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <param name="field">The field name.</param>
    /// <param name="minimum">The inclusive minimum, one for stored revisions.</param>
    /// <param name="maximum">The inclusive maximum, MaximumClock for clocks.</param>
    /// <returns>The unsigned value.</returns>
    /// <exception cref="CorruptDeliveryRow">The stored value is out of range.</exception>
    public static ulong ReadInt64(long value, string field, long minimum = 0, long maximum = MaximumCounter)
    {
        if (value < minimum || value > maximum)
        {
            throw new CorruptDeliveryRow(field);
        }

        return checked((ulong)value);
    }

    /// <summary>
    /// Restores an Int32 storage field after checking its range. Enums other than KeyKind are not narrowed.
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <param name="field">The field name.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>The validated stored value.</returns>
    /// <exception cref="CorruptDeliveryRow">The stored value is out of range.</exception>
    public static int ReadInt32(int value, string field, int minimum = 0, int maximum = int.MaxValue)
    {
        if (value < minimum || value > maximum)
        {
            throw new CorruptDeliveryRow(field);
        }

        return value;
    }
}
