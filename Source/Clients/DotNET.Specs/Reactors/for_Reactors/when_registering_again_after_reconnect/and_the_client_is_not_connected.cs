// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_the_client_is_not_connected : given.a_registered_delegate_that_was_disconnected
{
    int _whileDisconnected;

    void Establish() => _connectionLifecycle.IsConnected.Returns(false);

    async Task Because()
    {
        await _reactors.RegisterAgainAfterReconnect();
        _whileDisconnected = _definitions.Count;
        _connectionLifecycle.IsConnected.Returns(true);
        await _reactors.RegisterAgainAfterReconnect();
    }

    [Fact] void should_not_register_while_disconnected() => _whileDisconnected.ShouldEqual(1);
    [Fact] void should_keep_the_pending_registration_for_reconnect() => _definitions.Count.ShouldEqual(2);
}
