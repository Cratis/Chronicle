// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Classifies a strictly released value that carries a signed managed-key identity (v3) as released, known-erased, or a failure (#4619).
/// </summary>
/// <remarks>
/// A value is known-erased only when its certificate and value signature verify at the address it is released from and the fingerprint of the
/// embedded managed public key is recorded in the erasure fence. Nothing here probes retained key revisions, so an unprobed older live revision
/// can never be mistaken for an erased one. The signatures are verified on the live path too, so a value copied to another address fails closed
/// whether or not its key still exists. Values written before the identity envelope are not handled here and keep their earlier strict behavior.
/// </remarks>
internal static class ErasedValueClassifier
{
    /// <summary>
    /// Gets whether bytes are a v3 envelope this classifier handles.
    /// </summary>
    /// <param name="encrypted">The decoded stored value.</param>
    /// <returns>True for a v3 envelope.</returns>
    internal static bool Handles(byte[] encrypted) => EncryptionEnvelope.HasMagic(encrypted) && encrypted.Length > 4 && encrypted[4] is 3 or 4;

    /// <summary>
    /// Classifies and, when the key is live, releases a v3 value.
    /// </summary>
    /// <param name="storage">The key storage.</param>
    /// <param name="encryption">The encryption implementation.</param>
    /// <param name="address">The sole key identity the value may be released from.</param>
    /// <param name="encrypted">The decoded stored value.</param>
    /// <param name="stored">The stored value, returned unchanged when it is known-erased.</param>
    /// <returns>The released value with its status.</returns>
    /// <exception cref="EncryptionEnvelopeBindingFailed">The signatures, the embedded identity or the live key do not agree.</exception>
    /// <exception cref="MissingEncryptionKey">The key is missing and the value cannot be proven erased.</exception>
    internal static async Task<ReleasedProtectedValue> Classify(
        IEncryptionKeyStorage storage,
        IEncryption encryption,
        (EventStoreName EventStore, EventStoreNamespaceName Namespace, EncryptionKeyIdentifier Identifier) address,
        byte[] encrypted,
        JsonNode stored)
    {
        var envelope = EncryptionEnvelope.Read(encrypted);
        var fingerprint = EnvelopeSigning.Verify(envelope, new(address.EventStore, address.Namespace, address.Identifier));
        var fence = await ReleaseKeys.GetErasureForRelease(storage, address.EventStore, address.Namespace, address.Identifier);
        if (fence?.ErasedKeyFingerprints.Contains(fingerprint) == true)
        {
            return new(stored, ProtectedValueReleaseStatus.Erased);
        }

        var pair = await ReleaseKeys.TryGetPairForRelease(storage, address, envelope.Revision)
            ?? throw new MissingEncryptionKey(address.Identifier);
        if (pair.Key.Fingerprint != fingerprint)
        {
            throw new EncryptionEnvelopeBindingFailed("the key stored for the recorded revision is not the key the value was written under");
        }

        return new(ProtectedValueCodec.Decrypt(encryption, pair.Key, encrypted), ProtectedValueReleaseStatus.Decrypted);
    }
}
