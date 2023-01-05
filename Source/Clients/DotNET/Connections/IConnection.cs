// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Commands;
using Aksio.Cratis.Queries;

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents a connection.
/// </summary>
public interface IConnection
{
    /// <summary>
    /// Perform a command.
    /// </summary>
    /// <param name="route">Route of the command.</param>
    /// <param name="command">Optional command payload.</param>
    /// <returns><see cref="CommandResult"/> of the operation.</returns>
    Task<CommandResult> PerformCommand(string route, object? command = default);

    /// <summary>
    /// Perform a query.
    /// </summary>
    /// <param name="route">Route of the command.</param>
    /// <param name="queryString">Optional querystring.</param>
    /// <returns><see cref="QueryResult"/> of the operation.</returns>
    Task<QueryResult> PerformQuery(string route, IDictionary<string, string>? queryString = default);
}
