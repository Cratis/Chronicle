// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Binds the kernel's listener ports at process start and hands the bound sockets to Kestrel when it starts.
/// </summary>
/// <remarks>
/// The Chronicle port (35000) lies inside Linux's default ephemeral range (32768-60999). Building the host
/// already opens outgoing connections - MongoDB among them - and an outgoing connect() may be given the
/// listener port as its source port. That connection lives as long as the process, so Kestrel's later bind
/// fails with "address already in use" however long it retries (#5399). connect() never picks a port that
/// is explicitly bound, so binding before anything connects removes the race rather than narrowing it.
/// </remarks>
internal sealed class ReservedListenSockets : IDisposable
{
    readonly ConcurrentDictionary<IPEndPoint, Socket> _sockets = new();

    /// <summary>
    /// Releases every socket Kestrel did not take.
    /// </summary>
    public void Dispose()
    {
        foreach (var endpoint in _sockets.Keys)
        {
            if (_sockets.TryRemove(endpoint, out var socket))
            {
                socket.Dispose();
            }
        }
    }

    /// <summary>
    /// Binds every given port on the IPv6 any-address, the endpoint Kestrel binds for a listen-any-IP listener,
    /// or on the IPv4 any-address where IPv6 is unsupported, matching Kestrel's own fallback.
    /// </summary>
    /// <param name="ports">The listener ports.</param>
    /// <returns>The reserved sockets.</returns>
    internal static ReservedListenSockets Reserve(IEnumerable<int> ports) =>
        Reserve(ports, SocketTransportOptions.CreateDefaultBoundListenSocket);

    /// <summary>
    /// Binds every given port using the given socket factory, for specifying reservation without a real network port.
    /// </summary>
    /// <param name="ports">The listener ports.</param>
    /// <param name="createSocket">The socket factory.</param>
    /// <returns>The reserved sockets.</returns>
    internal static ReservedListenSockets Reserve(IEnumerable<int> ports, Func<EndPoint, Socket> createSocket)
    {
        var reserved = new ReservedListenSockets();
        foreach (var port in ports.Where(_ => _ > 0).Distinct())
        {
            try
            {
                reserved.Bind(createSocket, new IPEndPoint(IPAddress.IPv6Any, port));
            }
            catch (SocketException exception) when (exception.SocketErrorCode == SocketError.AddressFamilyNotSupported)
            {
                reserved.TryBind(createSocket, new IPEndPoint(IPAddress.Any, port));
            }
            catch (SocketException)
            {
                // Not reserved: Kestrel binds this port through its regular path, which retries
                // a transient conflict and names the holder of a persistent one.
            }
        }

        return reserved;
    }

    /// <summary>
    /// Takes the reserved socket for the endpoint Kestrel is binding, once.
    /// </summary>
    /// <param name="endpoint">The endpoint Kestrel is binding.</param>
    /// <returns>The reserved socket, or null when none was reserved for the endpoint.</returns>
    internal Socket? Take(EndPoint endpoint) =>
        endpoint is IPEndPoint ipEndPoint && _sockets.TryRemove(ipEndPoint, out var socket) ? socket : null;

    void Bind(Func<EndPoint, Socket> createSocket, IPEndPoint endpoint) => _sockets[endpoint] = createSocket(endpoint);

    void TryBind(Func<EndPoint, Socket> createSocket, IPEndPoint endpoint)
    {
        try
        {
            Bind(createSocket, endpoint);
        }
        catch (SocketException)
        {
            // Not reserved: Kestrel binds this port through its regular path.
        }
    }
}
