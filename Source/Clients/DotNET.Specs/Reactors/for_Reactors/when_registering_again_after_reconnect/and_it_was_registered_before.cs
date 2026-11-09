// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_it_was_registered_before : given.a_registered_delegate_that_was_disconnected
{
    Task Because() => _reactors.RegisterAgainAfterReconnect();

    [Fact] void should_register_on_both_connections() => _definitions.Count.ShouldEqual(2);
    [Fact] void should_register_the_recreated_handler() => ReferenceEquals(_reactors.GetHandlerById("bridge"), _original).ShouldBeFalse();
    [Fact] void should_not_cancel_the_recreated_handler() => _reactors.GetHandlerById("bridge").CancellationToken.IsCancellationRequested.ShouldBeFalse();
}
