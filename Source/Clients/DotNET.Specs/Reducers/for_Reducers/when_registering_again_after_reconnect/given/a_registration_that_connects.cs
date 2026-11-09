// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect.given;

public abstract class a_registration_that_connects : a_registered_reducer_that_was_disconnected
{
    protected Exception _error;
    Task _connected;
    int _connect;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(false);
        _servicesAccessor.Services.Returns(_ =>
        {
            if (Interlocked.Exchange(ref _connect, 0) == 1)
            {
                _lifecycle.IsConnected.Returns(true);
                _connected = Task.Run(_reducers.RegisterAgainAfterReconnect);
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
}
