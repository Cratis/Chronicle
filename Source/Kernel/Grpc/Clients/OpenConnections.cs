// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Services.Clients;

/// <summary>
/// Tracks the client connections open on this silo, and whether each client has kept responding to its keep-alives.
/// </summary>
/// <remarks>
/// <para>
/// The registry of connected clients lives in a grain activation, which can be lost while connections stay open - and a
/// new activation knows no client. The registry also drops a client on purpose when it stops responding for the stale
/// threshold. The registry cannot tell those apart once it has forgotten the client, so the evidence is kept here: only a
/// client that is still responding, with no gap in its responses long enough for it to have been dropped as stale, is
/// put back. A client that stopped responding stays dropped, even if it starts responding again.
/// </para>
/// <para>
/// A gap is measured from when the previous response was handled, not from when it arrived. The client waits for each
/// response to be handled before it can send the next, so time the server spends handling one - a registry call held up
/// while a silo leaves the cluster - is the server's delay, not the client's silence.
/// </para>
/// <para>
/// Shared by every call on the silo: the connection is opened by one call and the client responds through others.
/// </para>
/// </remarks>
internal sealed class OpenConnections
{
    readonly ConcurrentDictionary<ConnectionId, OpenConnection> _connections = new();

    /// <summary>
    /// Record that a connection was opened and its client registered.
    /// </summary>
    /// <param name="request">The <see cref="ConnectRequest"/> the client connected with.</param>
    /// <param name="at">When the client was registered.</param>
    public void Opened(ConnectRequest request, DateTimeOffset at) => _connections[request.ConnectionId] = new(request, at);

    /// <summary>
    /// Record that a connection was closed.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the connection.</param>
    public void Closed(ConnectionId connectionId) => _connections.TryRemove(connectionId, out _);

    /// <summary>
    /// Record that a response from the client was handled, which is when the client can send its next one.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> the client responded on.</param>
    /// <param name="at">When the response was handled.</param>
    public void Handled(ConnectionId connectionId, DateTimeOffset at)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            connection.Handled(at);
        }
    }

    /// <summary>
    /// Record that the client responded to a keep-alive, and decide whether the registry should get it back.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> the client responded on.</param>
    /// <param name="registryKnowsClient">Whether the registry of connected clients knew the client when it responded.</param>
    /// <param name="at">When the response arrived.</param>
    /// <param name="lapse">The longest gap between responses that cannot have got the client dropped as stale.</param>
    /// <param name="request">The <see cref="ConnectRequest"/> to restore the client from, when it should be restored.</param>
    /// <returns>True if the registry lost a client that is open here and has kept responding; false otherwise.</returns>
    public bool Responded(
        ConnectionId connectionId,
        bool registryKnowsClient,
        DateTimeOffset at,
        TimeSpan lapse,
        [NotNullWhen(true)] out ConnectRequest? request)
    {
        request = null;
        if (!_connections.TryGetValue(connectionId, out var connection))
        {
            return false;
        }

        if (!connection.Responded(registryKnowsClient, at, lapse))
        {
            return false;
        }

        request = connection.Request;
        return true;
    }

    sealed class OpenConnection(ConnectRequest request, DateTimeOffset openedAt)
    {
        readonly Lock _lock = new();
        DateTimeOffset _lastHandled = openedAt;
        bool _lapsed;

        public ConnectRequest Request { get; } = request;

        public void Handled(DateTimeOffset at)
        {
            lock (_lock)
            {
                _lastHandled = at;
            }
        }

        public bool Responded(bool registryKnowsClient, DateTimeOffset at, TimeSpan lapse)
        {
            lock (_lock)
            {
                var gap = at - _lastHandled;

                // The registry still holding the client settles it: whatever the gap, it was not dropped.
                if (registryKnowsClient)
                {
                    _lapsed = false;
                    return false;
                }

                // A gap this long may have got the client dropped as stale - and once it may have been, nothing
                // later on this connection can tell otherwise.
                if (gap >= lapse)
                {
                    _lapsed = true;
                }

                return !_lapsed;
            }
        }
    }
}
