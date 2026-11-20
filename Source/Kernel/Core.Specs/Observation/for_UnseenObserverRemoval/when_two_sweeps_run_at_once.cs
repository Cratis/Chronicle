// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_UnseenObserverRemoval;

public class when_two_sweeps_run_at_once : given.an_unseen_observer_removal
{
    readonly TaskCompletionSource _firstRemovalMayFinish = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int[] _results;

    async Task Because()
    {
        _removalGate = _firstRemovalMayFinish.Task;
        var first = _removal.Sweep();
        var second = _removal.Sweep();
        _firstRemovalMayFinish.SetResult();
        _results = await Task.WhenAll(first, second);
    }

    [Fact] void should_report_one_removal() => _results.Sum().ShouldEqual(1);
    [Fact] void should_ask_the_remover_once() => _removeCalls.ShouldEqual(1);
    [Fact] void should_remove_the_observer_once() => _removed.ShouldContainOnly("stale");

    protected override Task Seed() => Observer("stale", Old, Old);
}
