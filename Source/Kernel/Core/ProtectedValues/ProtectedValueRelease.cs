// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>Resolves a protected value without probing another identity or crossing an erasure fence.</summary>
/// <param name="storage">The managed key storage.</param>
/// <param name="encryption">The encryption implementation.</param>
/// <param name="address">The sole key identity permitted for this release.</param>
/// <param name="encrypted">The stored encrypted value.</param>
internal sealed class ProtectedValueRelease(
    IEncryptionKeyStorage storage,
    IEncryption encryption,
    (EventStoreName EventStore, EventStoreNamespaceName Namespace, EncryptionKeyIdentifier Identifier) address,
    byte[] encrypted)
{
    /// <summary>The maximum number of older revision slots to inspect.</summary>
    internal const int MaximumOlderRevisions = 16;

    /// <summary>Releases the value using its recorded revision or bounded revisionless fallback.</summary>
    /// <returns>The plaintext, or no value when its key is absent or erased.</returns>
    internal async Task<JsonNode?> Release()
    {
        if (encryption.TryGetKeyRevision(encrypted, out var revision))
        {
            return await ReleaseRecorded(revision);
        }

        var latest = await ReleaseKeys.TryGetPairForRelease(storage, address);
        if (latest is null) return null;
        try
        {
            return Decrypt(latest);
        }
        catch (EncryptionKeyUnwrapFailed failure)
        {
            return await ReleaseOlder(latest.Revision, failure);
        }
    }

    /// <summary>Preserves non-strict empty-value behavior when the erasure fence is unavailable.</summary>
    /// <returns>The released value, or no value when release cannot validate its fence.</returns>
    internal async Task<JsonNode?> ReleaseOrEmpty()
    {
        try
        {
            return await Release();
        }
        catch (EncryptionKeyFenceUnavailable error)
        {
            System.Diagnostics.Trace.TraceWarning("A protected value was released empty because its erasure fence could not be validated: {0}", error.Message);
            return null;
        }
    }

    async Task<JsonNode?> ReleaseRecorded(EncryptionKeyRevision revision)
    {
        var pair = await ReleaseKeys.TryGetPairForRelease(storage, address, revision);
        if (pair is null) return null;
        try
        {
            return Decrypt(pair);
        }
        catch (Exception failure) when (failure is EncryptionKeyUnwrapFailed or AuthenticationTagMismatchException)
        {
            throw new EncryptionKeyUnrecoverable(1, failure);
        }
    }

    async Task<JsonNode> ReleaseOlder(EncryptionKeyRevision latest, EncryptionKeyUnwrapFailed originalFailure)
    {
        var attempts = 1;
        var limit = Math.Min(MaximumOlderRevisions, latest.Value - 1);
        for (uint offset = 1; offset <= limit; offset++)
        {
            var pair = await ReleaseKeys.TryGetPairForRelease(storage, address, new(latest.Value - offset));
            if (pair is null) continue;
            attempts++;
            try
            {
                return Decrypt(pair);
            }
            catch (EncryptionKeyUnwrapFailed)
            {
                // Only an RSA unwrap failure permits trying the next same-identity revision.
            }
        }

        throw new EncryptionKeyUnrecoverable(attempts, originalFailure);
    }

    JsonNode Decrypt(EncryptionKeyAtRevision pair) => ProtectedValueCodec.Decrypt(encryption, pair.Key, encrypted);
}
