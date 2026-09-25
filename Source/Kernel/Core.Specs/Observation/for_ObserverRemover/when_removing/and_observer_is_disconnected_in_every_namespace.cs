// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

/// <summary>
/// The case the operation exists for: the declaring code is gone, no client reports the observer, and its records sit
/// in the store forever. Everything keyed to the observer has to go, in every namespace - a removal that leaves the
/// handled counts or the failed partitions behind is the same orphaned bookkeeping in a smaller pile.
/// </summary>
public class and_observer_is_disconnected_in_every_namespace : given.all_dependencies
{
    ObserverRemovalResult _result;

    async Task Because() => _result = await Remove();

    [Fact] void should_report_the_observer_as_removed() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);

    [Fact] async Task should_remove_the_observer_grain_in_the_first_namespace() => await _observerInFirstNamespace.Received(1).Remove();
    [Fact] async Task should_remove_the_observer_grain_in_the_second_namespace() => await _observerInSecondNamespace.Received(1).Remove();

    [Fact] async Task should_delete_the_store_level_definition() => await _observerDefinitions.Received(1).Delete(_observerId);

    [Fact] async Task should_delete_the_state_in_the_first_namespace() => await _firstNamespaceStorage.Observers.Received(1).Delete(_observerId);
    [Fact] async Task should_delete_the_state_in_the_second_namespace() => await _secondNamespaceStorage.Observers.Received(1).Delete(_observerId);

    [Fact] async Task should_remove_the_failed_partitions_in_every_namespace()
    {
        await _firstNamespaceStorage.FailedPartitions.Received(1).RemoveAllFor(_observerId);
        await _secondNamespaceStorage.FailedPartitions.Received(1).RemoveAllFor(_observerId);
    }

    [Fact] async Task should_remove_the_handled_counts_in_every_namespace()
    {
        await _firstNamespaceStorage.ObserverHandledCounts.Received(1).RemoveAllFor(_observerId);
        await _secondNamespaceStorage.ObserverHandledCounts.Received(1).RemoveAllFor(_observerId);
    }
}
