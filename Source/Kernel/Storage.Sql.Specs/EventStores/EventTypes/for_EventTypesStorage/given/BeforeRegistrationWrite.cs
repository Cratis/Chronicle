// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.given;

public sealed class BeforeRegistrationWrite(Func<Task> write) : DbCommandInterceptor
{
    public int Writes { get; private set; }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await BeforeWrite(command);
        return result;
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        await BeforeWrite(command);
        return result;
    }

    async Task BeforeWrite(DbCommand command)
    {
        if (!command.CommandText.Contains(WellKnownTableNames.EventTypes, StringComparison.Ordinal))
        {
            return;
        }

        if (command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal) || command.CommandText.StartsWith("INSERT", StringComparison.Ordinal))
        {
            Writes++;
            if (Writes == 1)
            {
                await write();
            }
        }
    }
}
