// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Identifies the algorithm, and for keyed constructions the key version, that produced the digests of a mutation receipt.
/// </summary>
/// <remarks>
/// The version is persisted with every receipt so that a later change of construction stays readable. Version
/// <see cref="Sha256DomainSeparated"/> is the unkeyed construction accepted by decision 0025.
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
    Sha256DomainSeparated = 1
}
