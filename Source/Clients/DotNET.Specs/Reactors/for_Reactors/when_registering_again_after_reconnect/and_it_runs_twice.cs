// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_it_runs_twice : given.a_registered_delegate_that_was_disconnected
{
    async Task Because()
    {
        await _reactors.RegisterAgainAfterReconnect();
        await _reactors.RegisterAgainAfterReconnect();
    }

    [Fact] void should_register_only_once_on_the_new_connection() => _definitions.Count.ShouldEqual(2);
}
