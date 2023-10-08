// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Kernel.Contracts.Clients;

namespace Aksio.Cratis.Kernel.Services.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionService"/>.
/// </summary>
public class ConnectionService : IConnectionService
{
    /// <inheritdoc/>
    public IObservable<ConnectionKeepAlive> Connect(ConnectRequest request)
    {
        return new Subject<ConnectionKeepAlive>();
    }

    /// <inheritdoc/>
    public void ConnectionKeepAlive(ConnectionKeepAlive keepAlive)
    {
    }
}
