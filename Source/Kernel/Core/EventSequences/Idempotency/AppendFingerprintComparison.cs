// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// Compares knowable intent without provisioning keys or mistaking a negative cache entry for erasure.
/// </summary>
internal static class AppendFingerprintComparison
{
    /// <summary>
    /// Compares the plain part first, then each stored key revision. Storage failures propagate unchanged.
    /// </summary>
    /// <param name="stored">The parsed durable fingerprint.</param>
    /// <param name="input">The retry's plaintext intent.</param>
    /// <param name="keys">The key storage.</param>
    /// <param name="cache">The local eviction seam.</param>
    /// <param name="store">The event store.</param>
    /// <param name="ns">The event store namespace.</param>
    /// <returns>The settlement, or an unsupported-boundary refusal.</returns>
    internal static async Task<Result<FingerprintComparisonResult, FingerprintFailure>> Compare(
        StoredAppendFingerprint stored,
        AppendFingerprintInput input,
        IEncryptionKeyStorage keys,
        IEvictEncryptionKeyCache cache,
        EventStoreName store,
        EventStoreNamespaceName ns)
    {
        if (!AppendFingerprint.Plain(input).TryGetResult(out var plain))
        {
            return FingerprintFailure.FingerprintUnsupported;
        }

        var groups = input.ProtectedValues.GroupBy(value => value.Kind).ToDictionary(group => group.Key, group => group.ToArray());
        if (plain != stored.Plain || !stored.ProtectedParts.Select(part => part.Kind).ToHashSet().SetEquals(groups.Keys))
        {
            var reason = stored.EventType.Id == input.EventType.Id && stored.EventType.Generation != input.EventType.Generation
                ? FingerprintConflictReason.Generation : FingerprintConflictReason.Content;
            return new FingerprintComparisonResult(FingerprintComparisonStatus.Conflict, stored.EventType, reason);
        }

        var erased = false;
        var unverifiable = false;
        foreach (var part in stored.ProtectedParts)
        {
            var values = groups[part.Kind];
            if (values.Select(value => value.Identifier).Distinct().Count() != 1)
            {
                return FingerprintFailure.FingerprintUnsupported;
            }

            var identifier = values[0].Identifier;
            var key = await keys.TryGetFor(store, ns, identifier, part.Revision);
            if (key is null)
            {
                cache.EvictFromCache(store, ns, identifier);
                key = await keys.TryGetFor(store, ns, identifier, part.Revision);
            }

            if (key is not null)
            {
                if (key.Fingerprint != part.KeyReference)
                {
                    unverifiable = true;
                }
                else if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(AppendFingerprint.ProtectedHash(input.Identity, key, values)),
                    Encoding.ASCII.GetBytes(part.Hash)))
                {
                    return new FingerprintComparisonResult(FingerprintComparisonStatus.Conflict, stored.EventType, FingerprintConflictReason.Content);
                }
            }
            else if (IsErased(part, await keys.GetErasureFor(store, ns, identifier)))
            {
                erased = true;
            }
            else
            {
                unverifiable = true;
            }
        }

        var status = (unverifiable, erased) switch
        {
            (true, _) => FingerprintComparisonStatus.Unverifiable,
            (false, true) => FingerprintComparisonStatus.VerifiedAfterErasure,
            _ => FingerprintComparisonStatus.Verified
        };
        return new FingerprintComparisonResult(status, stored.EventType);
    }

    static bool IsErased(ProtectedFingerprintPart part, EncryptionKeyErasure? erasure) =>
        erasure is not null && (part.Revision.Value <= erasure.ErasedThrough.Value || erasure.ErasedKeyFingerprints.Contains(part.KeyReference, StringComparer.Ordinal));
}
