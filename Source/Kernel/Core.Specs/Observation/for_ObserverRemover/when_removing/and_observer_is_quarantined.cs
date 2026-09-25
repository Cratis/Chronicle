// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

/// <summary>
/// A quarantined observer is stopped, not running, and quarantine is exactly the state an observer whose code has been
/// removed tends to end up in. Refusing here would leave the operator with an observer that can neither be recovered
/// nor removed.
/// </summary>
public class and_observer_is_quarantined : given.all_dependencies
{
    ObserverRemovalResult _result;

    void Establish()
    {
        ObserverIs(_observerInFirstNamespace, ObserverRunningState.Quarantined, false);
        ObserverIs(_observerInSecondNamespace, ObserverRunningState.Quarantined, false);
    }

    async Task Because() => _result = await Remove();

    [Fact] void should_remove_the_observer() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] async Task should_delete_the_store_level_definition() => await _observerDefinitions.Received(1).Delete(_observerId);
}
