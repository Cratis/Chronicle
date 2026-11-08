// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_registering_a_delegate_connects : given.a_registration_that_connects
{
    Task Because() => RegisterAndAwaitConnection();

    protected override Task RegisterWhileDisconnected() => _reactors.Register("other", builder => builder.WithEventType(new EventType("orders", 1)), (_, _) => Task.CompletedTask);

    [Fact] void should_complete_without_blocking_the_connected_handler() => _error.ShouldBeNull();
    [Fact] void should_register_both_reactors() => _definitions.Count.ShouldEqual(3);
}
