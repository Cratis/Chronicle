// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Derives deterministic <see cref="ApplicationId"/> values from a <see cref="ClientId"/>.
/// </summary>
/// <remarks>
/// The id is a name-based UUID (version 5, RFC 9562) of the client id in <see cref="Namespace"/>.
/// Every kernel node derives the same id for the same client id, so nodes that bootstrap
/// the same client concurrently against a shared store converge on one application.
/// The namespace must never change: doing so would give existing clients a new id.
/// </remarks>
internal static class ClientApplicationIds
{
    /// <summary>
    /// The fixed namespace for client application ids: 756e8114-4bff-4186-b1d0-0b211914d22a.
    /// </summary>
    internal static readonly Guid Namespace = new("756e8114-4bff-4186-b1d0-0b211914d22a");

    /// <summary>
    /// Gets the deterministic <see cref="ApplicationId"/> for a client id.
    /// </summary>
    /// <param name="clientId">The <see cref="ClientId"/> to derive the id from.</param>
    /// <returns>The deterministic <see cref="ApplicationId"/>.</returns>
    internal static ApplicationId For(ClientId clientId)
    {
        var name = Encoding.UTF8.GetBytes(clientId.Value);
        var input = new byte[16 + name.Length];
        Namespace.TryWriteBytes(input, bigEndian: true, out _);
        name.CopyTo(input, 16);

        Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];
#pragma warning disable CA5350 // RFC 9562 defines version 5 UUIDs with SHA-1; this is identity derivation, not security.
        SHA1.HashData(input, hash);
#pragma warning restore CA5350
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash[..16], bigEndian: true);
    }
}
