// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

public partial class Database
{
    async Task AdmitSchemaOnlyReadModel<TDbContext>(
        DatabaseIdentity identity,
        ConnectionPurpose purpose,
        TDbContext context,
        Func<DbContextOptions<TDbContext>, TDbContext> create,
        ConcurrentDictionary<string, CachedDbContextOptions<TDbContext>> cache,
        string key,
        Action<TDbContext>? admit)
        where TDbContext : DbContext
    {
        if (!_resolution.IsConfigured || purpose != ConnectionPurpose.Schema || context is not ReadModelDbContext schema) return;
        var target = await Target(identity, ConnectionPurpose.Runtime);
        await using var runtime = create(OptionsFor(cache, $"{key}:{nameof(ConnectionPurpose.Runtime)}", target));
        admit?.Invoke(runtime);
        if (runtime is not ReadModelDbContext readModel) throw new InvalidOperationException("Read-model storage is not available.");
        Sinks.SinkProvenanceTarget.For(readModel).Ensure(schema);
    }
}
