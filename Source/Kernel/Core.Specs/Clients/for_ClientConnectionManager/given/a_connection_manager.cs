// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Clients.for_ClientConnectionManager.given;

public class a_connection_manager : Specification
{
    protected IClientConnectionManager _manager;

    void Establish()
    {
        _manager = new ClientConnectionManager(NullLogger<ClientConnectionManager>.Instance);
    }

    protected static ConnectionId NewConnectionId() => (ConnectionId)Guid.NewGuid().ToString();
}
