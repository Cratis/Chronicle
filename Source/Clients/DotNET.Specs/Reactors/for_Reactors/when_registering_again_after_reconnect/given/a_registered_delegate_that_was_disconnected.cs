// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect.given;

public class a_registered_delegate_that_was_disconnected : for_Reactors.given.a_registered_delegate
{
    protected IReactorHandler _original;

    void Establish()
    {
        _connectionLifecycle.IsConnected.Returns(true);
        _original = _handler;
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
    }
}
