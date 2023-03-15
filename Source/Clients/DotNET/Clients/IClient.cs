// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Commands;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents a connection.
/// </summary>
public interface IClient
{
    /// <summary>
    /// Gets the unique <see cref="ConnectionId"/> for the client.
    /// </summary>
    ConnectionId ConnectionId { get; }

    /// <summary>
    /// Gets whether or not the client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connect to the kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Connect();

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
    /// <typeparam name="TResult">Type of the data within the result.</typeparam>
    /// <param name="route">Route of the command.</param>
    /// <param name="queryString">Optional querystring.</param>
    /// <returns><see cref="TypedQueryResult{T}"/> of the operation.</returns>
    Task<TypedQueryResult<TResult>> PerformQuery<TResult>(string route, IDictionary<string, string>? queryString = default);
}
