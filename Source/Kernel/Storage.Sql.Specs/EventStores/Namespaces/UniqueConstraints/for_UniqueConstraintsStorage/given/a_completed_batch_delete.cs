// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public sealed class a_completed_batch_delete : DbCommandInterceptor
{
    public int DeletedRows { get; private set; }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)) DeletedRows += result;
        return ValueTask.FromResult(result);
    }
}
