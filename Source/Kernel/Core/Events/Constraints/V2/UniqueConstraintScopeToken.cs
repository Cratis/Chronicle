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
    const int StackBufferSize = 512;
    const int HashSize = 32;

    /// <summary>
    /// Encodes a resolved scope, including the all-unnarrowed scope.
    /// </summary>
    /// <param name="key">The store HMAC key.</param>
    /// <param name="scope">The typed scope; null dimensions do not narrow it.</param>
    /// <returns>The scope token.</returns>
    internal static UniqueConstraintScopeToken Encode(ReadOnlySpan<byte> key, ResolvedConstraintScope? scope)
    {
        Span<byte> initial = stackalloc byte[StackBufferSize];
        var writer = new UniqueConstraintPreimageWriter(initial);
        try
        {
            WritePreimage(ref writer, scope);
            Span<byte> hash = stackalloc byte[HashSize];
            HMACSHA256.HashData(key, writer.Written, hash);
            return new(Convert.ToHexStringLower(hash[..16]));
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Builds the unhashed scope preimage from the event source type, event stream type and event stream identifier.
    /// </summary>
    /// <param name="scope">The typed scope; null dimensions are written as null.</param>
    /// <returns>The preimage bytes.</returns>
    internal static byte[] GetPreimage(ResolvedConstraintScope? scope)
    {
        Span<byte> initial = stackalloc byte[StackBufferSize];
        var writer = new UniqueConstraintPreimageWriter(initial);
        try
        {
            WritePreimage(ref writer, scope);
            return writer.Written.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    static void WritePreimage(ref UniqueConstraintPreimageWriter writer, ResolvedConstraintScope? scope)
    {
        writer.Write("chronicle/scope/v2\0"u8);
        UniqueConstraintFrame.WriteText(ref writer, scope?.EventSourceType?.Value);
        UniqueConstraintFrame.WriteText(ref writer, scope?.EventStreamType?.Value);
        UniqueConstraintFrame.WriteText(ref writer, scope?.EventStreamId?.Value);
    }
}
