// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Identifies the algorithm, and for keyed constructions the key version, that produced the digests of a mutation receipt.
/// </summary>
/// <remarks>
/// The version is persisted with every receipt so that a later change of construction stays readable. Version
/// <see cref="Sha256DomainSeparated"/> is the unkeyed construction first accepted by decision 0025 and is still read;
/// <see cref="HmacSha256PerEventStore"/> is the keyed construction new digests are written with.
/// </remarks>
public enum EventSequenceMutationDigestVersion
{
    /// <summary>
    /// The digest version has not been specified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Domain-separated SHA-256 over canonical length-prefixed frames. Unkeyed.
    /// </summary>
    Sha256DomainSeparated = 1,

    /// <summary>
    /// HMAC-SHA256 over the same canonical frames, keyed per event store with a key derived by HKDF-SHA256 from the
    /// event store's reserved mutation-receipt digest secret.
    /// </summary>
    HmacSha256PerEventStore = 2
}
