// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect.given;

public abstract class a_registered_reducer : a_connected_reducers
{
    protected IReducerHandler _original;

    async Task Establish() => _original = await _reducers.Register(typeof(First));
}
