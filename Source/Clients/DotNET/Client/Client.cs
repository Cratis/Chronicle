// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IClient"/>.
/// </summary>
public class Client : IClient
{
    readonly IConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IConnection"/> to use.</param>
    public Client(IConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public Task Connect()
    {
        return _connection.Connect();
    }
}
