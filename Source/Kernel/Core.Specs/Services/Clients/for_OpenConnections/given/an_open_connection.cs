// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_OpenConnections.given;

public class an_open_connection : Specification
{
    protected const string TheConnection = "the-client";
    protected static readonly TimeSpan _lapse = TimeSpan.FromSeconds(4);
    protected static readonly DateTimeOffset _openedAt = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    OpenConnections _connections;
    protected ConnectRequest _request;

    void Establish()
    {
        _connections = new();
        _request = new ConnectRequest { ConnectionId = TheConnection, ClientVersion = "1.0.0" };
        _connections.Opened(_request, _openedAt);
    }

    protected bool Responded(bool registryKnowsClient, TimeSpan after) => Responded(registryKnowsClient, after, out _);

    protected bool Responded(bool registryKnowsClient, TimeSpan after, out ConnectRequest? restoreFrom) =>
        Responded(registryKnowsClient, after, handledAfter: after, out restoreFrom);

    protected bool Responded(bool registryKnowsClient, TimeSpan after, TimeSpan handledAfter, out ConnectRequest? restoreFrom)
    {
        var restore = _connections.Responded(new ConnectionId(TheConnection), registryKnowsClient, _openedAt + after, _lapse, out restoreFrom);
        _connections.Handled(new ConnectionId(TheConnection), _openedAt + handledAfter);
        return restore;
    }

    protected void Close() => _connections.Closed(new ConnectionId(TheConnection));
}
