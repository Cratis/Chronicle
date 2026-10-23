// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage.given;

public sealed class InsertWinner : DbCommandInterceptor
{
    Func<Task>? _winner;

    public void Arm(Func<Task> winner) => _winner = winner;

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await InsertWinningRow(command);
        return result;
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        await InsertWinningRow(command);
        return result;
    }

    async Task InsertWinningRow(DbCommand command)
    {
        if (!command.CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase) &&
            !command.CommandText.Contains("MERGE", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var winner = _winner;
        _winner = null;
        if (winner is not null)
        {
            await winner();
        }
    }
}
