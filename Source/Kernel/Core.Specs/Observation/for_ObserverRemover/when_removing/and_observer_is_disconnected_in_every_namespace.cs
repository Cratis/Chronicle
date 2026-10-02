// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_observer_is_disconnected_in_every_namespace : given.all_dependencies
{
    ObserverRemovalResult _result;

    async Task Because() => _result = await Remove();

    [Fact] void should_report_the_observer_as_removed() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] async Task should_remove_the_observer_in_the_first_namespace() => await _observerInFirstNamespace.Received(1).Remove();
    [Fact] async Task should_remove_the_observer_in_the_second_namespace() => await _observerInSecondNamespace.Received(1).Remove();
    [Fact] async Task should_delete_the_store_level_definition() => await _observerDefinitions.Received(1).Delete(_observerId);
}
