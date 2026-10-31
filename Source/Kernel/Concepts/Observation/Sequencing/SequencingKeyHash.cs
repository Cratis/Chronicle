// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Represents the SHA-256 identity hash of canonical key bytes.
/// </summary>
public sealed record SequencingKeyHash : IComparable, IComparable<SequencingKeyHash>
{
    /// <summary>
    /// The required byte length of a SHA-256 key hash.
    /// </summary>
    public const int Length = 32;

    readonly byte[] _bytes;

    SequencingKeyHash(ReadOnlySpan<byte> bytes) => _bytes = bytes.ToArray();

    /// <summary>
    /// Gets the immutable hash bytes.
    /// </summary>
    public ReadOnlySpan<byte> Span => _bytes;

    /// <summary>
    /// Compares two hashes in ascending ordinal order.
    /// </summary>
    /// <param name="left">The first hash.</param>
    /// <param name="right">The second hash.</param>
    /// <returns>Whether the first hash precedes the second.</returns>
    public static bool operator <(SequencingKeyHash? left, SequencingKeyHash? right) => Comparer<SequencingKeyHash>.Default.Compare(left, right) < 0;

    /// <summary>
    /// Compares two hashes in descending ordinal order.
    /// </summary>
    /// <param name="left">The first hash.</param>
    /// <param name="right">The second hash.</param>
    /// <returns>Whether the first hash follows the second.</returns>
    public static bool operator >(SequencingKeyHash? left, SequencingKeyHash? right) => Comparer<SequencingKeyHash>.Default.Compare(left, right) > 0;

    /// <summary>
    /// Compares two hashes inclusively in ascending ordinal order.
    /// </summary>
    /// <param name="left">The first hash.</param>
    /// <param name="right">The second hash.</param>
    /// <returns>Whether the first hash does not follow the second.</returns>
    public static bool operator <=(SequencingKeyHash? left, SequencingKeyHash? right) => Comparer<SequencingKeyHash>.Default.Compare(left, right) <= 0;

    /// <summary>
    /// Compares two hashes inclusively in descending ordinal order.
    /// </summary>
    /// <param name="left">The first hash.</param>
    /// <param name="right">The second hash.</param>
    /// <returns>Whether the first hash does not precede the second.</returns>
    public static bool operator >=(SequencingKeyHash? left, SequencingKeyHash? right) => Comparer<SequencingKeyHash>.Default.Compare(left, right) >= 0;

    /// <summary>
    /// Hashes the complete canonical bytes including domain separators.
    /// </summary>
    /// <param name="key">The canonical key.</param>
    /// <returns>The SHA-256 key hash.</returns>
    /// <exception cref="InvalidSequencingKey">The key is missing.</exception>
    public static SequencingKeyHash From(CanonicalSequencingKey key)
    {
        if (key is null)
        {
            throw new InvalidSequencingKey("A canonical key is required to compute its hash.");
        }

        return new(SHA256.HashData(key.Span));
    }

    /// <summary>
    /// Restores a hash of exactly thirty-two bytes, copying the input.
    /// </summary>
    /// <param name="bytes">The stored hash bytes.</param>
    /// <returns>The immutable hash.</returns>
    /// <exception cref="InvalidCanonicalSequencingKey">The hash length is invalid.</exception>
    public static SequencingKeyHash FromStored(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Length)
        {
            throw new InvalidCanonicalSequencingKey("A key hash must contain exactly thirty-two bytes.");
        }

        return new(bytes);
    }

    /// <summary>
    /// Compares hashes ordinally by unsigned byte value.
    /// </summary>
    /// <param name="other">The hash to compare with.</param>
    /// <returns>The ordinal comparison, with a null hash ordered first.</returns>
    public int CompareTo(SequencingKeyHash? other) => other is null ? 1 : Span.SequenceCompareTo(other.Span);

    /// <inheritdoc/>
    /// <exception cref="InvalidSequencingKey">The compared object is not a sequencing key hash.</exception>
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        SequencingKeyHash hash => CompareTo(hash),
        _ => throw new InvalidSequencingKey("Only sequencing key hashes can be compared.")
    };

    /// <summary>
    /// Exports a fresh copy of the hash bytes.
    /// </summary>
    /// <returns>A mutable copy which cannot affect this hash.</returns>
    public byte[] ToArray() => _bytes.ToArray();

    /// <inheritdoc/>
    public bool Equals(SequencingKeyHash? other) => other is not null && Span.SequenceEqual(other.Span);

    /// <inheritdoc/>
    public override int GetHashCode() => unchecked((int)SequencingKeySliceHash.Hash(Span));

    /// <inheritdoc/>
    public override string ToString() => Convert.ToHexStringLower(Span);
}
