// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_UnseenObserverRemoval;

public class when_sweeping_again : given.an_unseen_observer_removal
{
    int _second;

    async Task Because()
    {
        await _removal.Sweep();

        // The removed observer's records are gone, as the remover leaves them.
        foreach (var @namespace in _states.Keys)
        {
            await _states[@namespace].Delete("stale");
        }

        _time.Now = _time.Now.AddMinutes(30);
        _second = await _removal.Sweep();
    }

    [Fact] void should_remove_nothing_the_second_time() => _second.ShouldEqual(0);
    [Fact] void should_have_asked_for_the_removal_only_once() => _removeCalls.ShouldEqual(1);
    [Fact] async Task should_keep_the_clock_that_the_first_sweep_started() =>
        (await _states[First].GetLastSeenAt(["never-seen"], _time.Now))["never-seen"].ShouldEqual(_time.Now.AddMinutes(-30));

    protected override async Task Seed()
    {
        await Observer("stale", Old, Old);
        await Observer("never-seen", null, null);
    }
}
