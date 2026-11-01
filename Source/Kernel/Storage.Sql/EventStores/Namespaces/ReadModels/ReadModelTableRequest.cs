// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// An explicit logical request; a physical result is never another request identity.
/// </summary>
/// <param name="ReadModelIdentifier">The registered model identifier.</param>
/// <param name="ContainerName">The original container, including for retained backups.</param>
/// <param name="Role">The requested role.</param>
/// <param name="LogicalName">The complete logical name for that role.</param>
internal sealed record ReadModelTableRequest(string ReadModelIdentifier, string ContainerName, ReadModelTableRole Role, string LogicalName)
{
    /// <summary>
    /// Attaches logical scope before any target-placement resolution.
    /// </summary>
    /// <param name="eventStoreId">The originating event store name.</param>
    /// <param name="namespaceName">The originating explicit namespace name.</param>
    /// <returns>The scoped owner.</returns>
    internal ReadModelTableOwner In(string eventStoreId, string namespaceName) =>
        new(eventStoreId, namespaceName, ReadModelIdentifier, ContainerName, Role, LogicalName);
}
