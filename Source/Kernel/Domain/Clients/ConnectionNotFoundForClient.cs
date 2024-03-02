// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Kernel.Domain.Clients;

/// <summary>
/// Exception that gets thrown when a connection is not found for a client.
/// </summary>
public class ConnectionNotFoundForClient : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionNotFoundForClient"/> class.
    /// </summary>
    /// <param name="connectionId"><see cref="ConnectionId"/> that is not found.</param>
    public ConnectionNotFoundForClient(ConnectionId connectionId)
        : base($"Connection '{connectionId}' not found for client")
    {
    }
}
