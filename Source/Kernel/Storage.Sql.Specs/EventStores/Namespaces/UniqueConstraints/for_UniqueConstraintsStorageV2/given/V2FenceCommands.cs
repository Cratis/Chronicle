// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2.given;

public class V2FenceCommands(V2Interleaving interleaving) : DbCommandInterceptor
{
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.Connection is SqlConnection && command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) && command.CommandText.Contains("UniqueConstraintAuthoritiesV2", StringComparison.Ordinal))
        {
            interleaving.TransitionReading();
        }

        return ValueTask.FromResult(result);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) && command.CommandText.Contains("UniqueConstraintAuthoritiesV2", StringComparison.Ordinal))
        {
            await interleaving.FencePassed();
        }

        return result;
    }
}
