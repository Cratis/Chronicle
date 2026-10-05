// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Records a committed replay promotion independently of table existence or identifier truncation.
/// </summary>
/// <param name="Id">The fixed-size lookup key for the full logical names.</param>
/// <param name="ContainerName">The complete logical read model container name.</param>
/// <param name="RevertContainerName">The complete, unique logical name for this replay's backup.</param>
/// <param name="BackupTableName">The physical backup this promotion still owns, or null if a later promotion reused that name.</param>
internal record ReplayPromotion(string Id, string ContainerName, string RevertContainerName, string? BackupTableName)
{
    /// <summary>
    /// Gets an identifier without truncating either logical name. The full names are also stored as values.
    /// </summary>
    /// <param name="containerName">The logical read model container name.</param>
    /// <param name="revertContainerName">The logical backup name, including the replay's timestamp and GUID.</param>
    /// <returns>The lookup key.</returns>
    public static string IdentifierFor(string containerName, string revertContainerName) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { containerName, revertContainerName }))));
}
