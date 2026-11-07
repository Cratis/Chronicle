// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the HMAC key that event sequence mutation digests of one event store are computed with.
/// </summary>
/// <remarks>
/// The key is never the stored secret itself: it is derived from it with HKDF-SHA256 and the fixed
/// <see cref="DerivationLabel"/> followed by the length-prefixed exact UTF-8 event store name. This binds the stored
/// material to both this purpose and this event store, even if stores receive identical secrets. The derived bytes are
/// held privately and are never exposed, logged or included in an exception.
/// </remarks>
public sealed class EventSequenceMutationDigestKey
{
    /// <summary>
    /// The HKDF info label that binds the derived key to mutation receipt digests.
    /// </summary>
    public const string DerivationLabel = "cratis.chronicle.mutation-receipt-digest.v2";

    /// <summary>
    /// The minimum length, in bytes, of the stored secret a key is derived from.
    /// </summary>
    public const int MinimumSecretLength = 32;

    const int KeyLength = 32;
    static readonly byte[] _label = Encoding.ASCII.GetBytes(DerivationLabel);
    readonly byte[] _key;

    EventSequenceMutationDigestKey(EventStoreName eventStore, byte[] key)
    {
        EventStore = eventStore;
        _key = key;
    }

    /// <summary>
    /// Gets the event store the key belongs to.
    /// </summary>
    public EventStoreName EventStore { get; }

    /// <summary>
    /// Derives the digest key of an event store from its stored secret.
    /// </summary>
    /// <param name="eventStore">The event store the secret belongs to.</param>
    /// <param name="secret">The stored secret; at least <see cref="MinimumSecretLength"/> bytes.</param>
    /// <returns>The derived key.</returns>
    /// <exception cref="ArgumentException">Thrown when the secret is shorter than <see cref="MinimumSecretLength"/>.</exception>
    public static EventSequenceMutationDigestKey DeriveFrom(EventStoreName eventStore, ReadOnlySpan<byte> secret)
    {
        if (secret.Length < MinimumSecretLength)
        {
            throw new ArgumentException($"The digest secret must be at least {MinimumSecretLength} bytes.", nameof(secret));
        }

        var storeBytes = Encoding.UTF8.GetBytes(eventStore.Value);
        var info = new byte[_label.Length + sizeof(int) + storeBytes.Length];
        _label.CopyTo(info, 0);
        BinaryPrimitives.WriteInt32BigEndian(info.AsSpan(_label.Length), storeBytes.Length);
        storeBytes.CopyTo(info, _label.Length + sizeof(int));

        var key = new byte[KeyLength];
        HKDF.DeriveKey(HashAlgorithmName.SHA256, secret, key, salt: [], info: info);
        return new(eventStore, key);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(EventSequenceMutationDigestKey)} for '{EventStore}'";

    /// <summary>
    /// Computes the HMAC-SHA256 of canonical bytes.
    /// </summary>
    /// <param name="canonicalBytes">The canonical bytes.</param>
    /// <returns>The 32-byte digest.</returns>
    internal byte[] Compute(ReadOnlySpan<byte> canonicalBytes) => HMACSHA256.HashData(_key, canonicalBytes);
}
