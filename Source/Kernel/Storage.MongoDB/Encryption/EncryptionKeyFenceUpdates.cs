// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// Guarded, monotone updates to a single fence document. A seal is computed from the CAS result, never a stale replacement.
/// </summary>
internal static class EncryptionKeyFenceUpdates
{
    static readonly FilterDefinitionBuilder<EncryptionKeyErasureForIdentifier> _filters = Builders<EncryptionKeyErasureForIdentifier>.Filter;
    static readonly UpdateDefinitionBuilder<EncryptionKeyErasureForIdentifier> _updates = Builders<EncryptionKeyErasureForIdentifier>.Update;

    /// <summary>
    /// Compute the rollout seal over the canonical fence fields.
    /// </summary>
    /// <param name="fence">The fence.</param>
    /// <returns>The SHA-256 digest.</returns>
    internal static string Seal(EncryptionKeyErasure fence) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        fence.Version,
        Through = fence.ErasedThrough.Value,
        Fingerprints = fence.ErasedKeyFingerprints.Order(StringComparer.Ordinal).ToArray(),
        fence.NewKeyAllowed,
        fence.Lifecycle,
        fence.ErasedLifecycle
    })));

    /// <summary>
    /// Match the observed version and lifecycle, including legacy absent fields.
    /// </summary>
    /// <param name="identifier">The subject.</param>
    /// <param name="fence">The observed state.</param>
    /// <returns>The CAS predicate.</returns>
    internal static FilterDefinition<EncryptionKeyErasureForIdentifier> Observed(EncryptionKeyIdentifier identifier, EncryptionKeyErasure fence)
    {
        var version = _filters.Eq(_ => _.Version, fence.Version);
        var lifecycle = _filters.Eq(_ => _.Lifecycle, fence.Lifecycle);
        if (fence.Version == 0)
        {
            version |= _filters.Exists(_ => _.Version, false);
        }

        if (fence.Lifecycle == 1)
        {
            lifecycle |= _filters.Exists(_ => _.Lifecycle, false);
        }

        return _filters.Eq(_ => _.Id, identifier) & version & lifecycle;
    }

    /// <summary>
    /// Initialize only an absent fence; existing documents are never overwritten.
    /// </summary>
    /// <param name="identifier">The subject.</param>
    /// <returns>The insert-only update.</returns>
    internal static UpdateDefinition<EncryptionKeyErasureForIdentifier> Ensure(EncryptionKeyIdentifier identifier)
    {
        var empty = new EncryptionKeyErasure(0u, [], true);
        return _updates.SetOnInsert(_ => _.Id, identifier)
            .SetOnInsert(_ => _.ErasedThrough, empty.ErasedThrough)
            .SetOnInsert(_ => _.ErasedKeyFingerprints, [])
            .SetOnInsert(_ => _.NewKeyAllowed, true)
            .SetOnInsert(_ => _.Version, 0UL)
            .SetOnInsert(_ => _.Lifecycle, 1u)
            .SetOnInsert(_ => _.ErasedLifecycle, 0u)
            .SetOnInsert(_ => _.Requests, [])
            .SetOnInsert(_ => _.Seal, Seal(empty));
    }

    /// <summary>
    /// Raise boundaries, union fingerprints and append one deduplicated request.
    /// </summary>
    /// <param name="next">The guarded result.</param>
    /// <param name="request">The immutable new request.</param>
    /// <returns>The monotone update.</returns>
    internal static UpdateDefinition<EncryptionKeyErasureForIdentifier> Raise(EncryptionKeyErasure next, EncryptionKeyErasureRequest request) =>
        Advance(next)
            .Max(_ => _.ErasedThrough, next.ErasedThrough)
            .Max(_ => _.ErasedLifecycle, next.ErasedLifecycle)
            .AddToSetEach(_ => _.ErasedKeyFingerprints, next.ErasedKeyFingerprints)
            .Set(_ => _.NewKeyAllowed, false)
            .Push(_ => _.Requests, request);

    /// <summary>
    /// Open exactly one successor lifecycle without changing any erasure boundary.
    /// </summary>
    /// <param name="next">The guarded result.</param>
    /// <returns>The authorization update.</returns>
    internal static UpdateDefinition<EncryptionKeyErasureForIdentifier> Authorize(EncryptionKeyErasure next) =>
        Advance(next).Set(_ => _.NewKeyAllowed, true);

    /// <summary>
    /// Merge discovered fingerprints or confirm one existing request without changing its boundary.
    /// </summary>
    /// <param name="next">The guarded result.</param>
    /// <param name="request">The updated request.</param>
    /// <returns>The request update.</returns>
    internal static UpdateDefinition<EncryptionKeyErasureForIdentifier> UpdateRequest(EncryptionKeyErasure next, EncryptionKeyErasureRequest request) =>
        Advance(next)
            .Set("requests.$", request)
            .AddToSetEach(_ => _.ErasedKeyFingerprints, next.ErasedKeyFingerprints);

    static UpdateDefinition<EncryptionKeyErasureForIdentifier> Advance(EncryptionKeyErasure next) =>
        _updates.Inc(_ => _.Version, 1UL)
            .Set(_ => _.Lifecycle, next.Lifecycle)
            .Set(_ => _.Seal, Seal(next));
}
