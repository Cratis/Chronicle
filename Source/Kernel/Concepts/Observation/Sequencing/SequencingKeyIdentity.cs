// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Represents mutually consistent canonical sequencing key identity columns.
/// </summary>
public sealed record SequencingKeyIdentity
{
    SequencingKeyIdentity(SequencingKeyKind kind, SequencingKeyHash hash, CanonicalSequencingKey key, SequencingKeySlice slice)
    {
        if (slice.Value >= SequencingKeySlice.Count)
        {
            throw new SequencingKeyIdentityMismatch("slice");
        }

        Kind = kind;
        Hash = hash;
        Key = key;
        Slice = slice;
    }

    /// <summary>
    /// Gets the canonical key domain.
    /// </summary>
    public SequencingKeyKind Kind { get; }

    /// <summary>
    /// Gets the SHA-256 hash of the canonical key.
    /// </summary>
    public SequencingKeyHash Hash { get; }

    /// <summary>
    /// Gets the canonical key bytes.
    /// </summary>
    public CanonicalSequencingKey Key { get; }

    /// <summary>
    /// Gets the frozen xxHash32 slice.
    /// </summary>
    public SequencingKeySlice Slice { get; }

    /// <summary>
    /// Creates a consistent identity from a logical key.
    /// </summary>
    /// <param name="key">The logical key.</param>
    /// <returns>The canonical identity.</returns>
    public static SequencingKeyIdentity For(SequencingKey key)
    {
        var canonical = CanonicalSequencingKey.From(key);

        return new((SequencingKeyKind)canonical.Span[0], SequencingKeyHash.From(canonical), canonical, SequencingKeySlice.From(canonical));
    }

    /// <summary>
    /// Restores identity columns after verifying kind, SHA-256 hash and slice against the canonical bytes.
    /// </summary>
    /// <param name="kind">The stored key kind.</param>
    /// <param name="hash">The stored key hash.</param>
    /// <param name="key">The stored canonical key.</param>
    /// <param name="slice">The stored slice.</param>
    /// <returns>The consistent identity.</returns>
    /// <exception cref="SequencingKeyIdentityMismatch">The stored columns disagree.</exception>
    public static SequencingKeyIdentity FromStored(SequencingKeyKind kind, SequencingKeyHash hash, CanonicalSequencingKey key, SequencingKeySlice slice)
    {
        if (key is null || (SequencingKeyKind)key.Span[0] != kind)
        {
            throw new SequencingKeyIdentityMismatch("kind");
        }

        if (hash is null || hash != SequencingKeyHash.From(key))
        {
            throw new SequencingKeyIdentityMismatch("hash");
        }

        if (slice is null || slice.Value >= SequencingKeySlice.Count || slice != SequencingKeySlice.From(key))
        {
            throw new SequencingKeyIdentityMismatch("slice");
        }

        return new(kind, hash, key, slice);
    }

    /// <summary>
    /// Restores raw stored identity columns, copying all byte buffers.
    /// </summary>
    /// <param name="kind">The stored key kind.</param>
    /// <param name="hash">The stored key hash bytes.</param>
    /// <param name="key">The stored canonical bytes.</param>
    /// <param name="slice">The stored slice number.</param>
    /// <returns>The consistent identity.</returns>
    /// <exception cref="SequencingKeyIdentityMismatch">A stored hash or slice is outside its range.</exception>
    public static SequencingKeyIdentity FromStored(SequencingKeyKind kind, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> key, ushort slice)
    {
        if (hash.Length != SequencingKeyHash.Length)
        {
            throw new SequencingKeyIdentityMismatch("hash");
        }

        if (slice >= SequencingKeySlice.Count)
        {
            throw new SequencingKeyIdentityMismatch("slice");
        }

        return FromStored(kind, SequencingKeyHash.FromStored(hash), CanonicalSequencingKey.FromStored(key), new SequencingKeySlice(slice));
    }
}
