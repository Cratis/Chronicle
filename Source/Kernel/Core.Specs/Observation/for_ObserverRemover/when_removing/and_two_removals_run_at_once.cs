// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

/// <summary>
/// Two callers remove the same observer at the same moment, as two sweeps of unseen observers can. Only one removes it: the other
/// finds it gone, so the removal is reported, logged and counted once.
/// </summary>
public class and_two_removals_run_at_once : given.all_dependencies
{
    readonly TaskCompletionSource _firstRemovalMayFinish = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverRemovalResult[] _results;

    void Establish()
    {
        _observerInFirstNamespace.Remove().Returns(_ => _firstRemovalMayFinish.Task);
        _observerDefinitions.Delete(_observerId).Returns(_ =>
        {
            _observerDefinitions.Has(_observerId).Returns(false);
            _firstNamespaceStorage.Observers.Get(_observerId).Returns(ObserverState.Empty);
            _secondNamespaceStorage.Observers.Get(_observerId).Returns(ObserverState.Empty);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        var first = Remove();
        var second = Remove();
        _firstRemovalMayFinish.SetResult();
        _results = await Task.WhenAll(first, second);
    }

    [Fact] void should_report_one_removal() => _results.Count(result => result.Outcome == ObserverRemovalOutcome.Removed).ShouldEqual(1);
    [Fact] void should_report_the_other_as_not_found() => _results.Count(result => result.Outcome == ObserverRemovalOutcome.ObserverNotFound).ShouldEqual(1);
    [Fact] async Task should_remove_the_observer_once() => await _observerInFirstNamespace.Received(1).Remove();
    [Fact] async Task should_delete_the_definition_once() => await _observerDefinitions.Received(1).Delete(_observerId);
}
