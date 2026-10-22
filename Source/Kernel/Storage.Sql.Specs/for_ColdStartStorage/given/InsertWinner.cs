// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage.given;

public sealed class InsertWinner(Func<Task> winner) : SaveChangesInterceptor
{
    bool _inserted;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!_inserted)
        {
            _inserted = true;
            await winner();
        }

        return result;
    }
}
