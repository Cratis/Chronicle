// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Cuts;

/// <summary>
/// Represents the 32-byte SHA-256 digest of one read model's captured payload.
/// </summary>
public sealed class ReadModelCutPayloadDigest : IEquatable<ReadModelCutPayloadDigest>
{
    const int Length = 32;

    readonly byte[] _bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelCutPayloadDigest"/> class.
    /// </summary>
    /// <param name="bytes">The 32 digest bytes to copy.</param>
    /// <exception cref="InvalidReadModelCutPayloadDigestLength">Thrown when <paramref name="bytes"/> does not contain exactly 32 bytes.</exception>
    public ReadModelCutPayloadDigest(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Length)
        {
            throw new InvalidReadModelCutPayloadDigestLength(bytes.Length);
        }

        _bytes = bytes.ToArray();
    }

    /// <summary>
    /// Determines whether two digests contain the same bytes.
    /// </summary>
    /// <param name="left">The left digest.</param>
    /// <param name="right">The right digest.</param>
    /// <returns><see langword="true"/> when the digests are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ReadModelCutPayloadDigest? left, ReadModelCutPayloadDigest? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two digests contain different bytes.
    /// </summary>
    /// <param name="left">The left digest.</param>
    /// <param name="right">The right digest.</param>
    /// <returns><see langword="true"/> when the digests are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ReadModelCutPayloadDigest? left, ReadModelCutPayloadDigest? right) => !Equals(left, right);

    /// <summary>
    /// Creates a defensive snapshot of the digest bytes.
    /// </summary>
    /// <returns>A new byte array containing the digest bytes.</returns>
    public byte[] Snapshot() => _bytes.ToArray();

    /// <summary>
    /// Gets the digest as a lowercase hexadecimal string, for display and storage keys.
    /// </summary>
    /// <returns>The hexadecimal representation of the digest.</returns>
    public override string ToString() => Convert.ToHexStringLower(_bytes);

    /// <inheritdoc/>
    public bool Equals(ReadModelCutPayloadDigest? other) => other is not null && _bytes.AsSpan().SequenceEqual(other._bytes);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ReadModelCutPayloadDigest other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.AddBytes(_bytes);
        return hash.ToHashCode();
    }
}
