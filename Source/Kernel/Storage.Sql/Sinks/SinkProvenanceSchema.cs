// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Opens the database-local provenance table through the configured migration/verification policy.
/// </summary>
internal static class SinkProvenanceSchema
{
    static readonly ProjectedColumn[] _columns = [new("Id", typeof(string), true, false, false, false)];

    /// <summary>
    /// Opens the reserved table on the primary connection. Verify mode validates rather than creating it.
    /// </summary>
    /// <param name="database">The resolved sink database.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The marker context scope.</returns>
    internal static Task<DbContextScope<ReadModelDbContext>> Open(IDatabase database, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        database.ReadModelTable(eventStore, @namespace, SinkProvenanceNames.Metadata, _columns);
}
