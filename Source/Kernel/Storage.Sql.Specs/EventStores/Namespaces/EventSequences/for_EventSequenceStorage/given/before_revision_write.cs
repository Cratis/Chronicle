// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

public class before_revision_write(Func<Task> collision, bool exhaust = false) : DbCommandInterceptor
{
    bool _collided;
    public int Attempts { get; private set; }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal) || !command.CommandText.Contains("Revisions", StringComparison.Ordinal)) return result;
        Attempts++;
        if (!_collided)
        {
            _collided = true;
            await collision();
        }
        return exhaust ? InterceptionResult<int>.SuppressWithResult(0) : result;
    }
}
