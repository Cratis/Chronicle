// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect.given;

public abstract class two_registered_reducers_that_were_disconnected : a_registered_reducer
{
    async Task Establish()
    {
        await _reducers.Register(typeof(Second));
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
    }
}
