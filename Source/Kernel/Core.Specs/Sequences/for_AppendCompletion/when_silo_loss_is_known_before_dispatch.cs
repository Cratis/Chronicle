// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendCompletion;

public class when_silo_loss_is_known_before_dispatch : given.an_append_endpoint
{
    readonly SiloUnavailableException _failure = new();
    Exception _exception;

    void Establish() => _principal.Current.Returns(_ => throw _failure);

    async Task Because() => _exception = await Catch.Exception(() =>
        new Append("store", "namespace", "event-log", "source", null!, null!, null!, new("event", 1, false), "{}")
            .Handle(_grainFactory, _causation, _principal));

    [Fact] void should_preserve_the_failure() => _exception.ShouldBeSame(_failure);
    [Fact] void should_not_dispatch_the_append() => _appendedEvents.ShouldBeNull();
}
