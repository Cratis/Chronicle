// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// Maps opaque identities to fixed-width SQL primary keys without collation-dependent equality.
/// </summary>
internal static class StorageIdentity
{
    /// <summary>
    /// Computes the database key for an identity.
    /// </summary>
    /// <param name="value">The opaque identity.</param>
    /// <returns>The uppercase SHA-256 digest.</returns>
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
