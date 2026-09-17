// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Represents the 32-byte version 1 digest of an event sequence mutation definition.
/// </summary>
public sealed class EventSequenceMutationDefinitionDigestV1 : IEquatable<EventSequenceMutationDefinitionDigestV1>
{
    readonly byte[] _bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceMutationDefinitionDigestV1"/> class.
    /// </summary>
    /// <param name="bytes">The 32 digest bytes to copy.</param>
    /// <exception cref="InvalidEventSequenceMutationDigestLength">Thrown when <paramref name="bytes"/> does not contain exactly 32 bytes.</exception>
    public EventSequenceMutationDefinitionDigestV1(ReadOnlySpan<byte> bytes) =>
        _bytes = EventSequenceMutationDigestBytes.CopyAndValidate<EventSequenceMutationDefinitionDigestV1>(bytes);

    /// <summary>
    /// Determines whether two digests contain the same bytes.
    /// </summary>
    /// <param name="left">The left digest.</param>
    /// <param name="right">The right digest.</param>
    /// <returns><see langword="true"/> when the digests are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(EventSequenceMutationDefinitionDigestV1? left, EventSequenceMutationDefinitionDigestV1? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two digests contain different bytes.
    /// </summary>
    /// <param name="left">The left digest.</param>
    /// <param name="right">The right digest.</param>
    /// <returns><see langword="true"/> when the digests are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(EventSequenceMutationDefinitionDigestV1? left, EventSequenceMutationDefinitionDigestV1? right) => !Equals(left, right);

    /// <summary>
    /// Creates a defensive snapshot of the digest bytes.
    /// </summary>
    /// <returns>A new byte array containing the digest bytes.</returns>
    public byte[] Snapshot() => _bytes.ToArray();

    /// <inheritdoc/>
    public bool Equals(EventSequenceMutationDefinitionDigestV1? other) => other is not null && _bytes.AsSpan().SequenceEqual(other._bytes);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is EventSequenceMutationDefinitionDigestV1 other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => EventSequenceMutationDigestBytes.GetStableHashCode(_bytes);
}
