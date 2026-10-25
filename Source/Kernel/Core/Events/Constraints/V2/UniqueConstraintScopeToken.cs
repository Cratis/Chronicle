// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Represents a fixed-length token for the three typed scope dimensions.
/// </summary>
/// <param name="Value">The first 32 lowercase hex characters of the scope HMAC.</param>
internal record UniqueConstraintScopeToken(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Encodes a resolved scope, including the all-unnarrowed scope.
    /// </summary>
    /// <param name="key">The store HMAC key.</param>
    /// <param name="scope">The typed scope; null dimensions do not narrow it.</param>
    /// <returns>The scope token.</returns>
    internal static UniqueConstraintScopeToken Encode(ReadOnlySpan<byte> key, ResolvedConstraintScope? scope)
    {
        var hash = HMACSHA256.HashData(key, GetPreimage(scope));
        return new(Convert.ToHexString(hash).ToLowerInvariant()[..32]);
    }

    internal static byte[] GetPreimage(ResolvedConstraintScope? scope)
    {
        using var target = new MemoryStream();
        target.Write("chronicle/scope/v2\0"u8);
        UniqueConstraintFrame.WriteText(target, scope?.EventSourceType?.Value);
        UniqueConstraintFrame.WriteText(target, scope?.EventStreamType?.Value);
        UniqueConstraintFrame.WriteText(target, scope?.EventStreamId?.Value);
        return target.ToArray();
    }
}
