// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_discovery_runs_while_disconnected : given.a_registered_delegate_that_was_disconnected
{
    async Task Because()
    {
        _connectionLifecycle.IsConnected.Returns(false);
        await _reactors.Discover();
        _connectionLifecycle.IsConnected.Returns(true);
        await _reactors.RegisterAgainAfterReconnect();
    }

    [Fact] void should_keep_the_pending_registration() => _definitions.Count.ShouldEqual(2);
}
