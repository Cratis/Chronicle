// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
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

        if (IsPlainRetry(stored, input, plain))
        {
            return new FingerprintComparisonResult(FingerprintComparisonStatus.Verified, stored.EventType);
        }

        var groups = input.ProtectedValues.GroupBy(value => value.Kind).ToDictionary(group => group.Key, group => group.ToArray());
        if (plain != stored.Plain || !stored.ProtectedParts.Select(part => part.Kind).ToHashSet().SetEquals(groups.Keys))
        {
            var reason = stored.EventType.Id == input.EventType.Id && stored.EventType.Generation != input.EventType.Generation
                ? FingerprintConflictReason.Generation : FingerprintConflictReason.Content;
            return new FingerprintComparisonResult(FingerprintComparisonStatus.Conflict, stored.EventType, reason);
        }

        using var scope = ReleaseKeys.BeginScope();
        var candidates = new List<ComparisonPart>();
        foreach (var part in stored.ProtectedParts.OrderBy(part => part.Kind))
        {
            var values = groups[part.Kind];
            if (values.Select(value => value.Identifier).Distinct().Count() != 1)
            {
                return FingerprintFailure.FingerprintUnsupported;
            }

            var identifier = values[0].Identifier;
            var pair = await keys.TryGetAtRevisionFor(store, ns, identifier, part.Revision);
            if (pair is null)
            {
                cache.EvictFromCache(store, ns, identifier);
                pair = await keys.TryGetAtRevisionFor(store, ns, identifier, part.Revision);
            }

            bool? matches = pair is not null && pair.Key.Fingerprint == part.KeyReference
                ? CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(AppendFingerprint.ProtectedHash(input.Identity, pair.Key, values)),
                    Encoding.ASCII.GetBytes(part.Hash))
                : null;
            var candidate = new ComparisonPart(part, pair, matches);
            candidates.Add(candidate);
            ReleaseKeys.ObserveForCompletion(keys, (store, ns, identifier), fence => candidate.Fence = fence);
            if (matches is false)
            {
                var settlement = await ReleaseKeys.Complete(() => Settle(stored, candidates));
                if (settlement.Status == FingerprintComparisonStatus.Conflict) return settlement;
            }
        }

        return await ReleaseKeys.Complete(() => Settle(stored, candidates));
    }

    static bool IsPlainRetry(StoredAppendFingerprint stored, AppendFingerprintInput input, string plain) =>
        stored.ProtectedParts.Count == 0 && input.ProtectedValues.Count == 0 && plain == stored.Plain;

    static FingerprintComparisonResult Settle(StoredAppendFingerprint stored, List<ComparisonPart> candidates)
    {
        var erased = false;
        ProtectionKind? unverifiable = null;
        foreach (var candidate in candidates)
        {
            if (IsErased(candidate.Part, candidate.Fence, candidate.Pair)) erased = true;
            else if (candidate.Matches is false) return new FingerprintComparisonResult(FingerprintComparisonStatus.Conflict, stored.EventType, FingerprintConflictReason.Content);
            else if (candidate.Matches is null) unverifiable ??= candidate.Part.Kind;
        }

        return new FingerprintComparisonResult(StatusFor(unverifiable, erased), stored.EventType, UnverifiableKind: unverifiable);
    }

    static FingerprintComparisonStatus StatusFor(ProtectionKind? unverifiable, bool erased) => (unverifiable.HasValue, erased) switch
    {
        (true, _) => FingerprintComparisonStatus.Unverifiable,
        (false, true) => FingerprintComparisonStatus.VerifiedAfterErasure,
        _ => FingerprintComparisonStatus.Verified
    };

    static bool IsErased(ProtectedFingerprintPart part, EncryptionKeyErasure? erasure, EncryptionKeyAtRevision? pair)
    {
        if (erasure is null)
        {
            return false;
        }

        if (pair is not null && erasure.Covers(pair))
        {
            return true;
        }

        return part.Revision.Value <= erasure.ErasedThrough.Value || erasure.ErasedKeyFingerprints.Contains(part.KeyReference, StringComparer.Ordinal);
    }

    sealed class ComparisonPart(ProtectedFingerprintPart part, EncryptionKeyAtRevision? pair, bool? matches)
    {
        public ProtectedFingerprintPart Part { get; } = part;
        public EncryptionKeyAtRevision? Pair { get; } = pair;
        public bool? Matches { get; } = matches;
        public EncryptionKeyErasure? Fence { get; set; }
    }
}
