// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage.given;

public sealed class RenameWinner(Func<Task> winner) : DbTransactionInterceptor
{
    bool _renamed;

    public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
    {
        if (!_renamed)
        {
            _renamed = true;
            await winner();
        }

        return result;
    }
}
