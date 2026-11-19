// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_UnseenObserverRemoval;

public class when_the_removal_is_turned_off : given.an_unseen_observer_removal
{
    int _result;

    protected override ChronicleOptions Configuration => new() { Observers = new() { UnseenRemovalAfter = TimeSpan.Zero } };

    async Task Because() => _result = await _removal.Sweep();

    [Fact] void should_remove_nothing() => _result.ShouldEqual(0);
    [Fact] void should_not_ask_the_remover_to_remove_anything() => _removeCalls.ShouldEqual(0);
    [Fact] async Task should_not_start_any_clock() =>
        (await _states[First].GetLastSeenAt(["never-seen"], DateTimeOffset.UnixEpoch))["never-seen"].ShouldEqual(DateTimeOffset.UnixEpoch);

    protected override async Task Seed()
    {
        await Observer("stale", Old, Old);
        await Observer("never-seen", null, null);
    }
}
