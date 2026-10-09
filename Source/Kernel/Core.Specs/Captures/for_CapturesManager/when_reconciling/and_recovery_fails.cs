// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_reconciling;

public class and_recovery_fails : given.a_captures_manager
{
    Exception _exception;

    void Establish()
    {
        StartedState(_events);
        _subscriptions.Recover(_eventStore, Arg.Any<CaptureDefinition>()).Returns<Task>(_ => throw new TimeoutException());
    }

    async Task Because() => _exception = await Catch.Exception(() => _silo.TimerRegistry.FireAllAsync());

    [Fact] void should_try_again_on_the_next_tick() => _exception.ShouldBeNull();
    [Fact] void should_have_attempted_recovery() => _subscriptions.Received(1).Recover(Arg.Any<EventStoreName>(), Arg.Any<CaptureDefinition>());
}
