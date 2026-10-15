// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_starting;

public class an_events_capture_that_fails_to_subscribe : given.a_captures_manager
{
    Exception _exception;

    void Establish() => _subscriptions.Subscribe(_eventStore, Arg.Any<CaptureDefinition>()).Returns<Task>(_ => throw new TimeoutException());

    async Task Because() => _exception = await Catch.Exception(() => _manager.Start(_events.Id));

    [Fact] void should_surface_the_failure() => _exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_not_record_it_as_started() => _captures.DidNotReceive().Save(Arg.Any<Capture>());
}
