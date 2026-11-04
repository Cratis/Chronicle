// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.given;

public sealed class CountsCommands : DbCommandInterceptor
{
    int _statements;
    public int Statements => _statements;
    public int FailAt { get; set; }
    public int LargestParameterCount { get; private set; }

    public void Reset()
    {
        _statements = 0;
        FailAt = 0;
        LargestParameterCount = 0;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) || command.CommandText.StartsWith("MERGE", StringComparison.OrdinalIgnoreCase))
        {
            var statement = Interlocked.Increment(ref _statements);
            LargestParameterCount = Math.Max(LargestParameterCount, command.Parameters.Count);
            if (statement == FailAt) throw new InvalidOperationException("Failure before the second count statement.");
        }

        return ValueTask.FromResult(result);
    }
}
