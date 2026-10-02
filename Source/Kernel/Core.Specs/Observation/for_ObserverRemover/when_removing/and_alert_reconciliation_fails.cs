// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_alert_reconciliation_fails : given.all_dependencies
{
    Exception _error;

    void Establish() => _observerInSecondNamespace.Remove().Returns(Task.FromException(new ObserverAlertsNotReconciled(new(_observerId, _eventStore, _secondNamespace, Concepts.EventSequences.EventSequenceId.Log))));

    async Task Because() => _error = await Catch.Exception(Remove);

    [Fact] void should_fail_management() => _error.ShouldBeOfExactType<ObserverAlertsNotReconciled>();
    [Fact] void should_not_delete_jobs_in_any_namespace() => _firstNamespaceJobs.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_not_delete_failed_partitions() => await _firstNamespaceStorage.FailedPartitions.DidNotReceive().RemoveAllFor(_observerId);
    [Fact] async Task should_not_delete_the_shared_definition() => await _observerDefinitions.DidNotReceive().Delete(_observerId);
    [Fact] async Task should_cancel_the_first_namespace_fence() => await _observerInFirstNamespace.Received(1).CancelRemoval();
    [Fact] async Task should_cancel_the_potentially_persisted_failing_fence() => await _observerInSecondNamespace.Received(1).CancelRemoval();
    [Fact] async Task should_not_release_any_removal_fence() => await _observerInFirstNamespace.DidNotReceive().CompleteRemoval();
}
