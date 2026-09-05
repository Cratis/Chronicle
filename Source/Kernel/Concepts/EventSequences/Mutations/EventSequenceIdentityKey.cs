// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Represents the strict UTF-8 key bytes for an event sequence identity.
/// </summary>
/// <remarks>
/// A default value is uninitialized and is distinct from an explicitly initialized empty key.
/// </remarks>
public readonly struct EventSequenceIdentityKey : IEquatable<EventSequenceIdentityKey>
{
    static readonly UTF8Encoding _strictUtf8 = new(false, true);
    readonly byte[]? _bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceIdentityKey"/> struct.
    /// </summary>
    /// <param name="bytes">The strict UTF-8 bytes to copy into the key.</param>
    /// <exception cref="InvalidEventSequenceIdentityKey">Thrown when <paramref name="bytes"/> are not strict, round-trippable UTF-8.</exception>
    public EventSequenceIdentityKey(ReadOnlySpan<byte> bytes)
    {
        try
        {
            var display = _strictUtf8.GetString(bytes);
            var roundTripped = _strictUtf8.GetBytes(display);
            if (!bytes.SequenceEqual(roundTripped))
            {
                throw new InvalidEventSequenceIdentityKey();
            }
        }
        catch (DecoderFallbackException)
        {
            throw new InvalidEventSequenceIdentityKey();
        }

        _bytes = bytes.ToArray();
    }

    /// <summary>
    /// Gets whether the key was explicitly initialized.
    /// </summary>
    public bool IsInitialized => _bytes is not null;

    /// <summary>
    /// Determines whether two keys have the same initialization state and byte content.
    /// </summary>
    /// <param name="left">The left key.</param>
    /// <param name="right">The right key.</param>
    /// <returns><see langword="true"/> when the keys are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(EventSequenceIdentityKey left, EventSequenceIdentityKey right) => left.Equals(right);

    /// <summary>
    /// Determines whether two keys differ in initialization state or byte content.
    /// </summary>
    /// <param name="left">The left key.</param>
    /// <param name="right">The right key.</param>
    /// <returns><see langword="true"/> when the keys are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(EventSequenceIdentityKey left, EventSequenceIdentityKey right) => !left.Equals(right);

    /// <summary>
    /// Creates a defensive snapshot of the key bytes.
    /// </summary>
    /// <returns>A new byte array containing the key bytes.</returns>
    public byte[] Snapshot() => _bytes?.ToArray() ?? [];

    /// <inheritdoc/>
    public bool Equals(EventSequenceIdentityKey other)
    {
        if (IsInitialized != other.IsInitialized)
        {
            return false;
        }

        if (_bytes is null)
        {
            return true;
        }

        return _bytes.AsSpan().SequenceEqual(other._bytes);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is EventSequenceIdentityKey other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;
        var hash = offsetBasis;
        unchecked
        {
            hash = (hash ^ (IsInitialized ? (byte)1 : (byte)0)) * prime;
            if (_bytes is not null)
            {
                foreach (var value in _bytes)
                {
                    hash = (hash ^ value) * prime;
                }
            }
        }

        return (int)hash;
    }
}
