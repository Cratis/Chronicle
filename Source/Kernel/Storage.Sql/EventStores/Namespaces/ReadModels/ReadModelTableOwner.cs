// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// The complete logical owner, independent of its resolved database and schema placement.
/// </summary>
/// <param name="EventStoreId">The original event store name.</param>
/// <param name="Namespace">The original explicit namespace name.</param>
/// <param name="ReadModelIdentifier">The registered model identifier.</param>
/// <param name="ContainerName">The original full live container name.</param>
/// <param name="Role">The table's role.</param>
/// <param name="LogicalName">The full logical table name.</param>
internal sealed record ReadModelTableOwner(
    string EventStoreId,
    string Namespace,
    string ReadModelIdentifier,
    string ContainerName,
    ReadModelTableRole Role,
    string LogicalName)
{
    /// <summary>
    /// Gets the fixed-size lookup key. Loaded rows must still match every full identity field.
    /// </summary>
    internal string Id => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new[] { EventStoreId, Namespace, ReadModelIdentifier, ContainerName, Role.ToString(), LogicalName }))));
}
