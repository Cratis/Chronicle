// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Encodes values and scopes with one store key, caching the token of the unscoped constraint.
/// </summary>
/// <remarks>
/// The instance is thread-safe. Dispose it to release the per-thread HMAC state; it must not be used afterwards. It copies the key, so later changes to the caller's buffer have no effect.
/// Each thread keeps its own reusable HMAC state, so the key padding is not recomputed on every call.
/// Scope tokens are cached per instance, because a scope repeats across appends; the cache is a fixed-size, direct-mapped array whose entries are overwritten in place, so it is bounded and never allocates after construction.
/// </remarks>
internal sealed class UniqueConstraintKeyedEncoder : IDisposable
{
    /// <summary>
    /// The number of scope token slots in the cache. Must be a power of two.
    /// </summary>
    internal const int ScopeCacheLimit = 1024;

    const int Stripes = 16;

    readonly ScopeSlot[] _slots = new ScopeSlot[ScopeCacheLimit];
    readonly Lock[] _locks = Enumerable.Range(0, Stripes).Select(_ => new Lock()).ToArray();
    readonly byte[] _key;
    readonly ThreadLocal<IncrementalHash> _hmac;
    readonly UniqueConstraintScopeToken _unscoped;
    int _occupied;
    int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueConstraintKeyedEncoder"/> class.
    /// </summary>
    /// <param name="key">The store HMAC key, which is copied.</param>
    internal UniqueConstraintKeyedEncoder(ReadOnlySpan<byte> key)
    {
        _key = key.ToArray();
        _hmac = new(() => IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, _key), trackAllValues: true);
        _unscoped = UniqueConstraintScopeToken.Encode(_key, null);
    }

    /// <summary>
    /// Gets the number of occupied scope cache slots, which never exceeds <see cref="ScopeCacheLimit"/>.
    /// </summary>
    internal int CachedScopeCount => Volatile.Read(ref _occupied);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }
        foreach (var hmac in _hmac.Values)
        {
            hmac.Dispose();
        }
        _hmac.Dispose();
    }

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
        var source = scope.EventSourceType?.Value;
        var streamType = scope.EventStreamType?.Value;
        var streamId = scope.EventStreamId?.Value;
        var index = HashCode.Combine(source, streamType, streamId) & (ScopeCacheLimit - 1);
        var gate = _locks[index & (Stripes - 1)];
        lock (gate)
        {
            ref var slot = ref _slots[index];
            if (slot.Token is not null && slot.Matches(source, streamType, streamId))
            {
                return slot.Token;
            }
        }
        var token = UniqueConstraintScopeToken.Encode(_hmac.Value!, scope);
        lock (gate)
        {
            ref var slot = ref _slots[index];
            if (slot.Token is null)
            {
                Interlocked.Increment(ref _occupied);
            }
            slot = new ScopeSlot(source, streamType, streamId, token);
        }
        return token;
    }

    readonly record struct ScopeSlot(string? EventSourceType, string? EventStreamType, string? EventStreamId, UniqueConstraintScopeToken? Token)
    {
        internal bool Matches(string? source, string? streamType, string? streamId) =>
            string.Equals(EventSourceType, source, StringComparison.Ordinal) &&
            string.Equals(EventStreamType, streamType, StringComparison.Ordinal) &&
            string.Equals(EventStreamId, streamId, StringComparison.Ordinal);
    }
}
