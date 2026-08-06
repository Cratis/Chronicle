// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Queries;

/// <summary>
/// Extension methods for working with <see cref="QueryResult{TData}"/>.
/// </summary>
public static class QueryResultExtensions
{
    /// <summary>
    /// Ensures the query executed successfully, returning its data or throwing if it did not.
    /// </summary>
    /// <param name="result">The <see cref="QueryResult{TData}"/> to check.</param>
    /// <typeparam name="TData">Type of data the query produces.</typeparam>
    /// <returns>The data produced by the query.</returns>
    /// <exception cref="QueryFailed">Thrown when the query did not succeed.</exception>
    public static TData EnsureSuccess<TData>(this QueryResult<TData> result)
    {
        if (!result.IsSuccess)
        {
            throw new QueryFailed(result.ValidationResults, result.ExceptionMessages);
        }

        return result.Data;
    }

    /// <summary>
    /// Awaits the query result and ensures the query executed successfully, returning its data or throwing if it did not.
    /// </summary>
    /// <param name="resultTask">The task producing the <see cref="QueryResult{TData}"/> to check.</param>
    /// <typeparam name="TData">Type of data the query produces.</typeparam>
    /// <returns>The data produced by the query.</returns>
    /// <exception cref="QueryFailed">Thrown when the query did not succeed.</exception>
    public static async Task<TData> EnsureSuccess<TData>(this Task<QueryResult<TData>> resultTask)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.EnsureSuccess();
    }
}
