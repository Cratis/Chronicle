// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Patterns;

/// <summary>
/// Provides the fixed-width hash a behavior pattern row is keyed by.
/// </summary>
/// <remarks>
/// SHA-256 rather than a cheaper non-cryptographic hash: this is a primary key, and two distinct facet sets
/// colliding would silently overwrite one pattern with another. It is not a security boundary - the property that
/// matters is that the value is stable across processes, runtimes and restarts, which rules out
/// <see cref="string.GetHashCode()"/>.
/// </remarks>
public static class FacetSetHash
{
    /// <summary>
    /// Gets the hash of a facet set key.
    /// </summary>
    /// <param name="key">The <see cref="FacetSetKey"/> to hash.</param>
    /// <returns>The lowercase hexadecimal hash.</returns>
    public static string Of(FacetSetKey key) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key.Value)));
}
