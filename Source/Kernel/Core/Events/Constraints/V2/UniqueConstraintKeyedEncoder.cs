// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Encodes values and scopes with one store key, caching the token of the unscoped constraint.
/// </summary>
/// <remarks>
/// The instance is thread-safe. It copies the key, so later changes to the caller's buffer have no effect.
/// Each thread keeps its own reusable HMAC state, so the key padding is not recomputed on every call.
/// Scope tokens are cached per instance, because a scope repeats across appends; the cache is bounded and is cleared when it is full.
/// </remarks>
internal sealed class UniqueConstraintKeyedEncoder
{
    /// <summary>
    /// The number of scope tokens kept before the cache is cleared.
    /// </summary>
    internal const int ScopeCacheLimit = 1024;

    readonly ConcurrentDictionary<ScopeParts, UniqueConstraintScopeToken> _scopes = new();
    int _scopeCount;
    readonly byte[] _key;
    readonly ThreadLocal<IncrementalHash> _hmac;
    readonly UniqueConstraintScopeToken _unscoped;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueConstraintKeyedEncoder"/> class.
    /// </summary>
    /// <param name="key">The store HMAC key, which is copied.</param>
    internal UniqueConstraintKeyedEncoder(ReadOnlySpan<byte> key)
    {
        _key = key.ToArray();
        _hmac = new(() => IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, _key));
        _unscoped = UniqueConstraintScopeToken.Encode(_key, null);
    }

    /// <summary>
    /// Gets the approximate number of cached scope tokens, which never exceeds the limit by more than the number of concurrent callers.
    /// </summary>
    internal int CachedScopeCount => _scopes.Count;

    /// <summary>
    /// Produces a keyed value, or no claim when every component is null.
    /// </summary>
    /// <param name="components">The schema-converted properties, in declaration order.</param>
    /// <param name="ignoreCasing">Whether to lowercase string leaves.</param>
    /// <returns>A 64-character lowercase hex value, or no claim.</returns>
    /// <exception cref="UnsupportedUniqueConstraintFormat">A format or nested array is unsupported.</exception>
    /// <exception cref="NonFiniteUniqueConstraintNumber">A number is not finite.</exception>
    internal Option<UniqueConstraintValue> EncodeValue(IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing = false) =>
        UniqueConstraintEncoder.Encode(_hmac.Value!, components, ignoreCasing);

    /// <summary>
    /// Encodes a resolved scope. An absent scope, or one that narrows no dimension, returns the cached unscoped token.
    /// </summary>
    /// <param name="scope">The typed scope; null dimensions do not narrow it.</param>
    /// <returns>The scope token.</returns>
    internal UniqueConstraintScopeToken EncodeScope(ResolvedConstraintScope? scope)
    {
        if (scope is null || (scope.EventSourceType is null && scope.EventStreamType is null && scope.EventStreamId is null))
        {
            return _unscoped;
        }
        var parts = new ScopeParts(scope.EventSourceType?.Value, scope.EventStreamType?.Value, scope.EventStreamId?.Value);
        if (_scopes.TryGetValue(parts, out var cached))
        {
            return cached;
        }
        var token = UniqueConstraintScopeToken.Encode(_hmac.Value!, scope);
        if (Interlocked.Increment(ref _scopeCount) > ScopeCacheLimit)
        {
            _scopes.Clear();
            Interlocked.Exchange(ref _scopeCount, 1);
        }
        _scopes[parts] = token;
        return token;
    }

    readonly record struct ScopeParts(string? EventSourceType, string? EventStreamType, string? EventStreamId);
}
