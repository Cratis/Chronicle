// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_registering_a_duplicate_id : given.a_registered_delegate
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _reactors.Register(
        "bridge",
        builder => builder.WithEventType(new EventType("different-event", 1)),
        (_, _) => Task.CompletedTask));

    [Fact] void should_reject_the_conflicting_subscription() => _exception.ShouldBeOfExactType<ReactorAlreadyRegistered>();
    [Fact] void should_preserve_the_existing_handler() => _reactors.GetHandlerById("bridge").ShouldEqual(_handler);
}
