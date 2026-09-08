// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Executes kernel queries, capturing the outcome of each result as a <see cref="QueryResult{TData}"/>.
/// </summary>
internal static class QueryExecutor
{
    /// <summary>
    /// Executes an observable query, wrapping every produced value in a <see cref="QueryResult{TData}"/>.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="onError">Optional callback invoked with any exception the query surfaces, before it is captured in the result.</param>
    /// <typeparam name="TData">Type of data the query produces.</typeparam>
    /// <returns>An observable of <see cref="QueryResult{TData}"/>.</returns>
    internal static IObservable<QueryResult<TData>> Execute<TData>(Func<IObservable<TData>> query, Action<Exception>? onError = null)
    {
        var correlationId = Guid.NewGuid();

        try
        {
            return query()
                .Select(data => QueryResult<TData>.Success(correlationId, data))
                .Catch<QueryResult<TData>, Exception>(ex => Observable.Return(Failed<TData>(correlationId, ex, onError)));
        }
        catch (Exception ex)
        {
            return Observable.Return(Failed<TData>(correlationId, ex, onError));
        }
    }

    /// <summary>
    /// Executes a query, capturing its outcome as a <see cref="QueryResult{TData}"/>.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="onError">Optional callback invoked with any exception the query surfaces, before it is captured in the result.</param>
    /// <typeparam name="TData">Type of data the query produces.</typeparam>
    /// <returns>The <see cref="QueryResult{TData}"/> describing the outcome.</returns>
    internal static async Task<QueryResult<TData>> Execute<TData>(Func<Task<TData>> query, Action<Exception>? onError = null)
    {
        var correlationId = Guid.NewGuid();

        try
        {
            return QueryResult<TData>.Success(correlationId, await query());
        }
        catch (Exception ex)
        {
            return Failed<TData>(correlationId, ex, onError);
        }
    }

    static QueryResult<TData> Failed<TData>(Guid correlationId, Exception exception, Action<Exception>? onError)
    {
        onError?.Invoke(exception);
        return QueryResult<TData>.Error(correlationId, exception);
    }
}
