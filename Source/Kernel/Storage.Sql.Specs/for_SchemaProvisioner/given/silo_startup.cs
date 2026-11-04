// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.given;

/// <summary>
/// Runs the SQL startup tasks in the order the silo runs them: the cluster first, then the read models once the names have been checked.
/// </summary>
public static class silo_startup
{
    public static async Task Run(Database database)
    {
        await new ClusterDbContextMigrator(database).Execute(CancellationToken.None);
        await new ReadModelSchemaVerification(database, NullLogger<ReadModelSchemaVerification>.Instance).Execute(CancellationToken.None);
    }
}
