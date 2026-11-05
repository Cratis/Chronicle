// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Describes how long the identifiers of a closed stream may be to fit the key and the columns they are stored in.
/// </summary>
public static class ClosedStreamKeyLength
{
    /// <summary>
    /// The longest value one identifier column holds, in characters.
    /// </summary>
    public const int MaxColumnLength = 255;

    /// <summary>
    /// The most bytes the identifiers of a stored key may use, leaving room for the dimensions of the key within the 900 byte limit of a key.
    /// </summary>
    public const int MaxKeyBytes = 868;

    /// <summary>
    /// The most characters the identifiers of a stored key may total.
    /// </summary>
    public const int MaxTotalLength = (MaxKeyBytes - 4) / 2;

    /// <summary>
    /// Gets whether the identifiers of a closed stream fit the stored key.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence the stream belongs to.</param>
    /// <param name="scope">The scope that is closed. Parts that are not set count as empty.</param>
    /// <param name="owner">The owner of the closure.</param>
    /// <returns>True when every identifier fits its column and the key fits its limit.</returns>
    public static bool Fits(EventSequenceId eventSequenceId, ClosedStreamScope scope, string owner)
    {
        int[] lengths =
        [
            eventSequenceId.Value.Length,
            scope.EventStreamType?.Value.Length ?? 0,
            scope.EventStreamId?.Value.Length ?? 0,
            scope.EventSourceId?.Value.Length ?? 0,
            scope.EventSourceType?.Value.Length ?? 0,
            owner.Length
        ];

        return lengths.All(length => length <= MaxColumnLength) && lengths.Sum() <= MaxTotalLength;
    }
}
