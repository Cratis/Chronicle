// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Holds immutable secret material for an event store's unique-constraint encoding.
/// </summary>
/// <remarks>
/// Automatic JSON serialization and deserialization are explicitly rejected, including in containing records.
/// Automatic BSON serialization exposes no material. Persistence must use explicit copy-out, never automatic
/// serialization of this type. Diagnostics, including containing records, are redacted.
/// </remarks>
[JsonConverter(typeof(UniqueConstraintStoreKeyJsonConverter))]
public sealed class UniqueConstraintStoreKey
{
    /// <summary>
    /// The required size of the HMAC key in bytes.
    /// </summary>
    public const int Size = 32;

    readonly byte[] _bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueConstraintStoreKey"/> class by copying the material.
    /// </summary>
    /// <param name="bytes">Exactly 256 bits of key material.</param>
    /// <exception cref="InvalidUniqueConstraintStoreKeyLength">The material is not exactly 32 bytes.</exception>
    public UniqueConstraintStoreKey(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Size)
        {
            throw new InvalidUniqueConstraintStoreKeyLength();
        }

        _bytes = bytes.ToArray();
    }

    /// <summary>
    /// Copies the secret material for explicit encoding or persistence use.
    /// </summary>
    /// <returns>A new buffer owned by the caller, which must not be logged or automatically serialized.</returns>
    public byte[] CopyBytes() => [.. _bytes];

    /// <inheritdoc/>
    public override string ToString() => "[REDACTED]";
}
