// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Reserves a loopback port until its container is ready to claim a fixed mapping.
/// </summary>
internal sealed class HostPortReservation : IDisposable
{
    readonly TcpListener _listener = new(IPAddress.Loopback, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="HostPortReservation"/> class.
    /// </summary>
    internal HostPortReservation()
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
    }

    /// <summary>
    /// Gets the port selected once for the lifetime of the run.
    /// </summary>
    internal int Port { get; }

    /// <inheritdoc/>
    public void Dispose() => _listener.Dispose();
}
