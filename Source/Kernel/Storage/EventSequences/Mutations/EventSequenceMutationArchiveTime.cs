// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Normalises archive timestamps so that they round-trip exactly through every provider as Unix milliseconds.
/// </summary>
public static class EventSequenceMutationArchiveTime
{
    /// <summary>
    /// Normalises a timestamp to UTC with millisecond precision.
    /// </summary>
    /// <param name="value">The timestamp.</param>
    /// <returns>The normalised timestamp.</returns>
    public static DateTimeOffset Normalize(DateTimeOffset value) => DateTimeOffset.FromUnixTimeMilliseconds(value.ToUnixTimeMilliseconds());

    /// <summary>
    /// Determines whether a timestamp is already in its normalised form.
    /// </summary>
    /// <param name="value">The timestamp.</param>
    /// <returns>True if it is UTC, has millisecond precision and is not before the Unix epoch.</returns>
    public static bool IsNormalized(DateTimeOffset value) =>
        value >= DateTimeOffset.UnixEpoch && value == Normalize(value) && value.Offset == TimeSpan.Zero;
}
