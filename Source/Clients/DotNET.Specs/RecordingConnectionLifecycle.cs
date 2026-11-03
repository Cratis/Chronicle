// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle;

/// <summary>
/// An <see cref="IConnectionLifecycle"/> that counts who is subscribed, so a specification can prove that a store let go
/// of the shared connection. A substitute cannot do that, because it forgets every handler it is given.
/// </summary>
public class RecordingConnectionLifecycle : IConnectionLifecycle
{
    readonly object _sync = new();
    readonly List<Delegate> _connected = [];
    readonly List<Delegate> _disconnected = [];
    int _connectedAdds;
    int _disconnectedAdds;

    /// <inheritdoc/>
    public event Connected OnConnected
    {
        add
        {
            lock (_sync)
            {
                _connected.Add(value);
                _connectedAdds++;
            }
        }

        remove
        {
            lock (_sync) _connected.Remove(value);
        }
    }

    /// <inheritdoc/>
    public event Disconnected OnDisconnected
    {
        add
        {
            lock (_sync)
            {
                _disconnected.Add(value);
                _disconnectedAdds++;
            }
        }

        remove
        {
            lock (_sync) _disconnected.Remove(value);
        }
    }

    /// <inheritdoc/>
    public bool IsConnected => false;

    /// <inheritdoc/>
    public ConnectionId ConnectionId { get; } = new("recording-connection");

    /// <summary>
    /// Gets the number of handlers currently subscribed to <see cref="OnConnected"/>.
    /// </summary>
    public int ConnectedCount
    {
        get
        {
            lock (_sync) return _connected.Count;
        }
    }

    /// <summary>
    /// Gets the number of handlers currently subscribed to <see cref="OnDisconnected"/>.
    /// </summary>
    public int DisconnectedCount
    {
        get
        {
            lock (_sync) return _disconnected.Count;
        }
    }

    /// <summary>
    /// Gets how many handlers have ever been added to <see cref="OnConnected"/>.
    /// </summary>
    public int TotalConnectedAdds
    {
        get
        {
            lock (_sync) return _connectedAdds;
        }
    }

    /// <summary>
    /// Gets how many handlers have ever been added to <see cref="OnDisconnected"/>.
    /// </summary>
    public int TotalDisconnectedAdds
    {
        get
        {
            lock (_sync) return _disconnectedAdds;
        }
    }

    /// <inheritdoc/>
    public Task Connected()
    {
        Delegate[] handlers;
        lock (_sync) handlers = [.. _connected];
        return Task.WhenAll(handlers.Cast<Connected>().Select(_ => _()));
    }

    /// <inheritdoc/>
    public Task Disconnected()
    {
        Delegate[] handlers;
        lock (_sync) handlers = [.. _disconnected];
        return Task.WhenAll(handlers.Cast<Disconnected>().Select(_ => _()));
    }
}
