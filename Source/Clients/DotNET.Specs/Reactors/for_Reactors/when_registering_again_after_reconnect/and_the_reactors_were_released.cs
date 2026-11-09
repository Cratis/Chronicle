// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_the_reactors_were_released : given.a_registered_delegate_that_was_disconnected
{
    void Establish() => _reactors.Release();

    Task Because() => _reactors.RegisterAgainAfterReconnect();

    [Fact] void should_not_register_the_reactor_again() => _definitions.Count.ShouldEqual(1);
}
