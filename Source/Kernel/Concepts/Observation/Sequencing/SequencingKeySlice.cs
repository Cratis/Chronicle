// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Represents one of the frozen 1024 sequencing key slices.
/// </summary>
public sealed record SequencingKeySlice : ConceptAs<ushort>
{
    /// <summary>
    /// The number of slices, fixed independently of runtime lane counts.
    /// </summary>
    public const int Count = 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencingKeySlice"/> class.
    /// </summary>
    /// <param name="value">The zero-based slice number.</param>
    /// <exception cref="InvalidSequencingKey">The slice is outside the supported range.</exception>
    public SequencingKeySlice(ushort value) : base(value)
    {
        if (value >= Count)
        {
            throw new InvalidSequencingKey("A sequencing key slice must be between zero and 1023.");
        }
    }

    /// <summary>
    /// Computes the frozen seed-zero xxHash32 slice from canonical bytes.
    /// </summary>
    /// <param name="key">The canonical key.</param>
    /// <returns>The slice of the key.</returns>
    /// <exception cref="InvalidSequencingKey">The key is missing.</exception>
    public static SequencingKeySlice From(CanonicalSequencingKey key)
    {
        if (key is null)
        {
            throw new InvalidSequencingKey("A canonical key is required to compute its slice.");
        }

        return new((ushort)(SequencingKeySliceHash.Hash(key.Span) % Count));
    }
}
