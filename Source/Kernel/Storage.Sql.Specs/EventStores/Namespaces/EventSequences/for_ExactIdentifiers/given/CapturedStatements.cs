// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers.given;

/// <summary>
/// Records the text of every command the context would run, without opening a connection or running it.
/// </summary>
public sealed class CapturedStatements
{
    readonly ConcurrentQueue<string> _statements = new();

    public IReadOnlyList<string> All => [.. _statements];

    public void Clear() => _statements.Clear();

    public IInterceptor[] CreateInterceptors() => [new Commands(_statements), new Connection()];

    sealed class Connection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    sealed class Commands(ConcurrentQueue<string> statements) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            statements.Enqueue(command.CommandText);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(new DataTable().CreateDataReader()));
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            statements.Enqueue(command.CommandText);
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
        }
    }
}
