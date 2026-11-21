// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_UnseenObserverRemoval;

public class when_the_period_is_not_positive : given.an_unseen_observer_removal
{
    int _result;

    protected override ChronicleOptions Configuration => new() { Observers = new() { UnseenRemovalEnabled = true, UnseenRemovalAfter = TimeSpan.Zero } };

    async Task Because() => _result = await _removal.Sweep();

    [Fact] void should_use_the_seven_day_period() => _result.ShouldEqual(1);
    [Fact] void should_remove_the_observer_unseen_for_longer_than_seven_days() => _removed.ShouldContain("stale");
    [Fact] void should_keep_the_observer_seen_within_seven_days() => _removed.ShouldNotContain("recent");

    protected override async Task Seed()
    {
        await Observer("stale", Old, Old);
        await Observer("recent", Recent, Recent);
    }
}
