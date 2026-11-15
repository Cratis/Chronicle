// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Pairs an event store's stable encoding-key identity with its immutable secret material.
/// </summary>
/// <param name="KeyId">The non-secret key identity.</param>
/// <param name="Material">The secret material, which must be persisted only through explicit copy-out.</param>
public sealed record UniqueConstraintEncodingKey(UniqueConstraintStoreKeyId KeyId, UniqueConstraintStoreKey Material)
{
    /// <summary>
    /// Creates an independent 128-bit random identity and 256-bit random key using a cryptographic generator.
    /// </summary>
    /// <returns>A newly generated encoding key. This does not persist or initialize any storage.</returns>
    public static UniqueConstraintEncodingKey Create() => new(
        new UniqueConstraintStoreKeyId(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()),
        new UniqueConstraintStoreKey(RandomNumberGenerator.GetBytes(UniqueConstraintStoreKey.Size)));
}
