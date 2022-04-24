// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Extensions.Orleans.Execution;
using Orleans;

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents a <see cref="IOutgoingGrainCallFilter"/> for adding the connection identifier to the outgoing call context.
/// </summary>
public class ConnectionIdOutgoingCallFilter : IOutgoingGrainCallFilter
{
    readonly IRequestContextManager _requestContextManager;
    readonly IConnectionManager _connectionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionIdOutgoingCallFilter"/> class.
    /// </summary>
    /// <param name="requestContextManager"><see cref="IRequestContextManager"/> for working with state in requests.</param>
    /// <param name="connectionManager"><see cref="IConnectionManager"/> for getting current connection identifier.</param>
    public ConnectionIdOutgoingCallFilter(
        IRequestContextManager requestContextManager,
        IConnectionManager connectionManager)
    {
        _requestContextManager = requestContextManager;
        _connectionManager = connectionManager;
    }

    /// <inheritdoc/>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        _requestContextManager!.Set(
                RequestContextKeys.ConnectionId,
                _connectionManager.ConnectionId);
        await context.Invoke();
    }
}
