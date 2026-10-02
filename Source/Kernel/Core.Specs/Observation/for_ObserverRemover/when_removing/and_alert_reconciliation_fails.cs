// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_alert_reconciliation_fails : given.all_dependencies
{
    Exception _error;
    ObserverAlertsNotReconciled _failure;

    void Establish()
    {
        _failure = new(new(_observerId, _eventStore, _secondNamespace, Concepts.EventSequences.EventSequenceId.Log));
        _observerInSecondNamespace.Remove().Returns(Task.FromException(_failure));
    }

    async Task Because() => _error = await Catch.Exception(Remove);

    [Fact] void should_propagate_the_original_failure() => _error.ShouldEqual(_failure);
    [Fact] async Task should_have_removed_the_earlier_namespace() => await _observerInFirstNamespace.Received(1).Remove();
    [Fact] async Task should_not_delete_the_shared_definition() => await _observerDefinitions.DidNotReceive().Delete(_observerId);
}
