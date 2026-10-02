// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_cleanup_fails_after_alerts_are_cleared : given.all_dependencies
{
    Exception _error;
    readonly Exception _failure = new("Shared cleanup failed");

    void Establish() => _observerDefinitions.Delete(_observerId).Returns(Task.FromException(_failure));

    async Task Because() => _error = await Catch.Exception(Remove);

    [Fact] void should_propagate_the_original_failure() => _error.ShouldEqual(_failure);
    [Fact] async Task should_have_removed_the_first_namespace() => await _observerInFirstNamespace.Received(1).Remove();
    [Fact] async Task should_have_removed_the_second_namespace() => await _observerInSecondNamespace.Received(1).Remove();
}
