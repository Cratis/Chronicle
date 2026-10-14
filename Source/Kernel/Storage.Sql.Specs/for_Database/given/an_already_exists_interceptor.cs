// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class an_already_exists_interceptor : DbCommandInterceptor
{
    public int Failures { get; private set; }
    public int HistoryReads { get; private set; }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("__EFMigrationsHistory", StringComparison.Ordinal))
        {
            HistoryReads++;
            if (Failures == 0)
            {
                Failures++;
                throw new PostgresException("already exists (injected migration failure)", "ERROR", "ERROR", PostgresErrorCodes.DuplicateTable);
            }
        }

        return ValueTask.FromResult(result);
    }
}
