// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect.given;

public class two_registered_delegates_that_were_disconnected : a_registered_delegate_that_was_disconnected
{
    async Task Establish()
    {
        await _reactors.Register("other", builder => builder.WithEventType(new EventType("orders", 1)), (_, _) => Task.CompletedTask);
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
    }
}
