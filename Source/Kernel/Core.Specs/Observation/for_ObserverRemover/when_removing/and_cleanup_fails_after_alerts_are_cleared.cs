// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_cleanup_fails_after_alerts_are_cleared : given.all_dependencies
{
    Exception _error;

    void Establish() => _observerDefinitions.Delete(_observerId).Returns(Task.FromException(new InvalidOperationException("Shared cleanup failed")));

    async Task Because() => _error = await Catch.Exception(Remove);

    [Fact] void should_propagate_the_failure() => _error.ShouldNotBeNull();
    [Fact] async Task should_have_applied_the_first_namespace_removal() => await _observerInFirstNamespace.Received(1).Remove();
    [Fact] async Task should_have_applied_the_second_namespace_removal() => await _observerInSecondNamespace.Received(1).Remove();
    [Fact] async Task should_not_cancel_the_first_fence() => await _observerInFirstNamespace.DidNotReceive().CancelRemoval();
    [Fact] async Task should_not_cancel_the_second_fence() => await _observerInSecondNamespace.DidNotReceive().CancelRemoval();
    [Fact] async Task should_keep_the_first_removal_fence() => await _observerInFirstNamespace.DidNotReceive().CompleteRemoval();
    [Fact] async Task should_keep_the_second_removal_fence() => await _observerInSecondNamespace.DidNotReceive().CompleteRemoval();
}
