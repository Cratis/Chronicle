// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage.given;

public sealed class RenameWinner : DbTransactionInterceptor
{
    Func<Task>? _winner;

    public void Arm(Func<Task> winner) => _winner = winner;

    public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
    {
        var winner = _winner;
        _winner = null;
        if (winner is not null)
        {
            await winner();
        }

        return result;
    }
}
