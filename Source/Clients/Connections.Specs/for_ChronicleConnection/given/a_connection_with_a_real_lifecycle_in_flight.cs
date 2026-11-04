// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// A connection driven by the real lifecycle, so generations, the disconnect fence and the connection id behave as they do in production.
/// The lifecycle reports when the connection starts waiting for a disconnect to complete, which is the one point a spec can
/// observe a connect being held without watching the clock.
/// </summary>
public class a_connection_with_a_real_lifecycle_in_flight : a_connection_with_a_connect_in_flight
{
    protected ObservableLifecycle _real;
    protected TaskCompletionSource _fenceAwaited;

    protected override IConnectionLifecycle CreateRealLifecycle()
    {
        _fenceAwaited = NewGate();
        _real = new ObservableLifecycle(new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance), _fenceAwaited);
        return _real;
    }

    protected sealed class ObservableLifecycle(IConnectionLifecycle inner, TaskCompletionSource fenceAwaited) : IConnectionLifecycle
    {
        public event Connected OnConnected
        {
            add => inner.OnConnected += value;
            remove => inner.OnConnected -= value;
        }

        public event Disconnected OnDisconnected
        {
            add => inner.OnDisconnected += value;
            remove => inner.OnDisconnected -= value;
        }

        public bool IsConnected => inner.IsConnected;

        public ConnectionId ConnectionId => inner.ConnectionId;

        public Task Connected() => inner.Connected();

        public Task Disconnected() => inner.Disconnected();

        public Task DisconnectCompleted()
        {
            fenceAwaited.TrySetResult();
            return inner.DisconnectCompleted();
        }
    }
}
