// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// An EF Core interceptor that counts every command that reaches the database, by SQL verb. Transaction control
/// (BEGIN/COMMIT) goes through the EF transaction API and is not counted.
/// </summary>
/// <param name="counter">The <see cref="CommandCounter"/> to count into.</param>
internal sealed class SqlCommandCounter(CommandCounter counter) : DbCommandInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Count(command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Count(command);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Count(command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Count(command);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Count(command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        Count(command);
        return ValueTask.FromResult(result);
    }

    /// <summary>Gets the verb a command text starts with.</summary>
    /// <param name="text">The command text.</param>
    /// <returns>The lower-case verb, or `other`.</returns>
    internal static string VerbOf(string text)
    {
        var trimmed = text.AsSpan().TrimStart();
        var end = 0;
        while (end < trimmed.Length && char.IsLetter(trimmed[end])) end++;
        return end == 0 ? "other" : trimmed[..end].ToString().ToLowerInvariant();
    }

    void Count(DbCommand command) => counter.Record(VerbOf(command.CommandText));
}
