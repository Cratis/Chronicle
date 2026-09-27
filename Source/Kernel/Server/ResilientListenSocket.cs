// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Binds Kestrel after transient source-port collisions during Orleans startup have cleared.
/// </summary>
internal static class ResilientListenSocket
{
    /// <summary>
    /// Binds a socket, retrying transient address-in-use errors until the timeout expires.
    /// </summary>
    /// <param name="endpoint">The address to bind.</param>
    /// <param name="timeout">The retry budget.</param>
    /// <param name="logger">The kernel logger.</param>
    /// <returns>The bound socket.</returns>
    internal static Socket Bind(EndPoint endpoint, TimeSpan timeout, ILogger<Kernel> logger) =>
        Bind(endpoint, timeout, logger, SocketTransportOptions.CreateDefaultBoundListenSocket);

    /// <summary>
    /// Binds a socket using the given socket factory, for specifying retries without a real network port.
    /// </summary>
    /// <param name="endpoint">The address to bind.</param>
    /// <param name="timeout">The retry budget.</param>
    /// <param name="logger">The kernel logger.</param>
    /// <param name="createSocket">The socket factory.</param>
    /// <returns>The bound socket.</returns>
    internal static Socket Bind(EndPoint endpoint, TimeSpan timeout, ILogger<Kernel> logger, Func<EndPoint, Socket> createSocket)
    {
        var stopwatch = Stopwatch.StartNew();
        var reported = false;
        while (true)
        {
            try
            {
                return createSocket(endpoint);
            }
            catch (SocketException exception) when (exception.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                if (!reported || stopwatch.Elapsed >= timeout)
                {
                    logger.PortBindConflict(endpoint, TcpPortHolders.Describe(endpoint));
                    reported = true;
                }

                if (stopwatch.Elapsed >= timeout)
                {
                    throw;
                }

                // An outbound socket without SO_REUSEADDR can briefly own Kestrel's port.
                // Only retry EADDRINUSE; a persistent listener still fails within the budget.
                Thread.Sleep(TimeSpan.FromMilliseconds(250));
            }
        }
    }
}
