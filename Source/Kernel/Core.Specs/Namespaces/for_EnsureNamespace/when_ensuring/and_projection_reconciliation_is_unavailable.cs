// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_EnsureNamespace.when_ensuring;

public class and_projection_reconciliation_is_unavailable : given.an_ensure_command
{
    readonly Exception _failure = new TimeoutException();
    Exception _exception;

    void Establish() => _projections.EnsureSubscribedIn(_namespace).Returns(Task.FromException(_failure));

    async Task Because() => _exception = await Catch.Exception(() => _command.Handle(_grains));

    [Fact] void should_propagate_the_manager_failure() => _exception.ShouldBeSame(_failure);
    [Fact] async Task should_also_attempt_the_capture_subscriptions() => await _captures.Received(1).EnsureSubscribedIn(_namespace);
}
