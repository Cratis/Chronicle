// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect.given;

public abstract class a_registration_that_connects : a_registered_delegate_that_was_disconnected
{
    protected Exception _error;
    Task _connected;
    int _connect;

    void Establish()
    {
        _connectionLifecycle.IsConnected.Returns(false);
        _eventTypes.GetEventTypeFor(typeof(OrderPlaced)).Returns(new EventType("orders", 1));
        _servicesAccessor.Services.Returns(_ =>
        {
            if (Interlocked.Exchange(ref _connect, 0) == 1)
            {
                _connectionLifecycle.IsConnected.Returns(true);
                _connected = Task.Run(_reactors.RegisterAgainAfterReconnect);
                _connected.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            }
            return _services;
        });
        _connect = 1;
    }

    protected async Task RegisterAndAwaitConnection()
    {
        _error = await Catch.Exception(RegisterWhileDisconnected);
        await _connected.WaitAsync(TimeSpan.FromSeconds(10));
    }

    protected abstract Task RegisterWhileDisconnected();

    protected class AnotherReactor : IReactor;
    protected record OrderPlaced(string OrderNumber);
}
