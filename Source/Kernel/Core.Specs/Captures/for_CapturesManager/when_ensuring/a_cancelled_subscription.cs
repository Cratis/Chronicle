// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring;

public class a_cancelled_subscription : given.a_captures_manager
{
    Exception _exception;

    void Establish()
    {
        StartedState(_events);
        _subscriptions.Subscribe(_eventStore, Arg.Any<CaptureDefinition>()).Returns<Task>(_ => throw new OperationCanceledException());
    }

    async Task Because() => _exception = await Catch.Exception(() => _manager.Ensure());

    [Fact] void should_not_swallow_the_cancellation() => _exception.ShouldBeOfExactType<OperationCanceledException>();
}
