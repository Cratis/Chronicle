// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_a_namespace_is_added;

public class and_the_subscription_fails : given.a_captures_manager
{
    Exception _exception;

    void Establish()
    {
        StartedState(_events);
        _subscriptions.Subscribe(_eventStore, "new-tenant", Arg.Any<CaptureDefinition>()).Returns<Task>(_ => throw new TimeoutException());
    }

    async Task Because() => _exception = await Catch.Exception(() => _manager.NamespaceAdded("new-tenant"));

    [Fact] void should_leave_it_to_reconciliation() => _exception.ShouldBeNull();
}
