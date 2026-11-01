// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Defines where read model tables for an event store and namespace live.
/// </summary>
internal interface IReadModelTablePlacements
{
    /// <summary>
    /// Locates the read model database of an event store and namespace for a connection purpose.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <param name="purpose">The connection purpose.</param>
    /// <returns>The <see cref="ReadModelTablePlace"/>.</returns>
    ValueTask<ReadModelTablePlace> Locate(string eventStore, string namespaceName, ConnectionPurpose purpose);
}
