// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Commands;
using Aksio.Cratis.Queries;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClient"/> for usage inside a silo.
/// </summary>
public class InsideSiloClient : IClient
{
    /// <inheritdoc/>
    public bool IsConnected => false;

    /// <inheritdoc/>
    public ConnectionId ConnectionId => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Connect() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<CommandResult> PerformCommand(string route, object? command = null) => Task.FromResult(CommandResult.Success);

    /// <inheritdoc/>
    public Task<QueryResult> PerformQuery(string route, IDictionary<string, string>? queryString = null) => Task.FromResult(QueryResult.Success);
}
