// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// Counts every command EF Core sends to the database. It observes commands and never replaces the connection, so
/// the kernel sees the provider's own connection type, exactly as in production.
/// </summary>
/// <param name="counter">The counter to record into.</param>
internal sealed class CountingCommandInterceptor(CommandCounter counter) : DbCommandInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result) => Count(command, result);

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Count(command, result));

    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result) => Count(command, result);

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Count(command, result));

    /// <inheritdoc/>
    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result) => Count(command, result);

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Count(command, result));

    InterceptionResult<T> Count<T>(DbCommand command, InterceptionResult<T> result)
    {
        counter.Record(SqlVerb.Of(command.CommandText));
        return result;
    }
}
