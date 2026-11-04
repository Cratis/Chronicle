// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect.given;

public abstract class a_registered_reducer_that_was_disconnected : a_registered_reducer
{
    void Establish() => _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
}
