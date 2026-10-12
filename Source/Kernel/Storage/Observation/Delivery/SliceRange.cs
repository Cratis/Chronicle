// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents a non-empty half-open range of the frozen 1024 key slices.
/// </summary>
public sealed record SliceRange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SliceRange"/> class.
    /// </summary>
    /// <param name="from">The inclusive start.</param>
    /// <param name="to">The exclusive end, at most 1024.</param>
    public SliceRange(ushort from, ushort to)
    {
        DeliveryRanges.Slices(from, to);
        From = from;
        To = to;
    }

    /// <summary>
    /// Gets the inclusive start.
    /// </summary>
    public ushort From { get; }

    /// <summary>
    /// Gets the exclusive end.
    /// </summary>
    public ushort To { get; }

    /// <summary>
    /// Gets the range covering all slices.
    /// </summary>
    public static SliceRange All => new(0, SequencingKeySlice.Count);

    /// <summary>
    /// Tests membership using half-open interval semantics.
    /// </summary>
    /// <param name="slice">The slice to test.</param>
    /// <returns>Whether the slice is in this range.</returns>
    public bool Contains(SequencingKeySlice slice) => slice.Value >= From && slice.Value < To;
}
