// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Encodes values and scopes with one store key, caching the token of the unscoped constraint.
/// </summary>
/// <remarks>
/// The instance is immutable and thread-safe. It copies the key, so later changes to the caller's buffer have no effect.
/// </remarks>
internal sealed class UniqueConstraintKeyedEncoder
{
    readonly byte[] _key;
    readonly UniqueConstraintScopeToken _unscoped;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueConstraintKeyedEncoder"/> class.
    /// </summary>
    /// <param name="key">The store HMAC key, which is copied.</param>
    internal UniqueConstraintKeyedEncoder(ReadOnlySpan<byte> key)
    {
        _key = key.ToArray();
        _unscoped = UniqueConstraintScopeToken.Encode(_key, null);
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
        UniqueConstraintEncoder.Encode(_key, components, ignoreCasing);

    /// <summary>
    /// Encodes a resolved scope. An absent scope, or one that narrows no dimension, returns the cached unscoped token.
    /// </summary>
    /// <param name="scope">The typed scope; null dimensions do not narrow it.</param>
    /// <returns>The scope token.</returns>
    internal UniqueConstraintScopeToken EncodeScope(ResolvedConstraintScope? scope) =>
        scope is null || (scope.EventSourceType is null && scope.EventStreamType is null && scope.EventStreamId is null)
            ? _unscoped
            : UniqueConstraintScopeToken.Encode(_key, scope);
}
