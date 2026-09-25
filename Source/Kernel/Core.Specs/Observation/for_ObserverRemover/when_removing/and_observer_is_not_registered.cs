// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_observer_is_not_registered : given.all_dependencies
{
    ObserverRemovalResult _result;

    void Establish()
    {
        _observerDefinitions.Has(_observerId).Returns(false);
        _firstNamespaceStorage.Observers.Get(_observerId).Returns(ObserverState.Empty);
        _secondNamespaceStorage.Observers.Get(_observerId).Returns(ObserverState.Empty);
    }

    async Task Because() => _result = await Remove();

    [Fact] void should_report_the_observer_as_not_found() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.ObserverNotFound);
    [Fact] async Task should_not_delete_the_definition() => await _observerDefinitions.DidNotReceive().Delete(Arg.Any<ObserverId>());
    [Fact] async Task should_not_remove_any_observer_grain() => await _observerInFirstNamespace.DidNotReceive().Remove();
}
