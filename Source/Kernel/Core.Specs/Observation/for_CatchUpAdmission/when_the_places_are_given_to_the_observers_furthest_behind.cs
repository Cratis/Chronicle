// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.for_CatchUpAdmission;

public class when_the_places_are_given_to_the_observers_furthest_behind : given.a_catch_up_admission
{
    IDisposable? _smallBacklogWhileFull;
    IDisposable? _largeBacklogWhileFull;
    IDisposable? _smallBacklogWhenAPlaceIsFree;
    IDisposable? _largeBacklogWhenAPlaceIsFree;
    IDisposable? _smallBacklogWhileTheLargeOneStarts;

    async Task Because()
    {
        RunningJobs(CatchUpOf("running"));
        _smallBacklogWhileFull = await _admission.TryAdmit(KeyOf("small"), 1, 10);
        _largeBacklogWhileFull = await _admission.TryAdmit(KeyOf("large"), 1, 5_000);

        RunningJobs(CatchUpOf("running", JobStatus.CompletedSuccessfully));
        _smallBacklogWhenAPlaceIsFree = await _admission.TryAdmit(KeyOf("small"), 1, 10);
        _largeBacklogWhenAPlaceIsFree = await _admission.TryAdmit(KeyOf("large"), 1, 5_000);
        _smallBacklogWhileTheLargeOneStarts = await _admission.TryAdmit(KeyOf("small"), 1, 10);
    }

    [Fact] void should_hold_back_the_small_backlog_while_full() => _smallBacklogWhileFull.ShouldBeNull();
    [Fact] void should_hold_back_the_large_backlog_while_full() => _largeBacklogWhileFull.ShouldBeNull();
    [Fact] void should_keep_the_free_place_for_the_observer_furthest_behind() => _smallBacklogWhenAPlaceIsFree.ShouldBeNull();
    [Fact] void should_admit_the_observer_furthest_behind_when_a_place_is_free() => _largeBacklogWhenAPlaceIsFree.ShouldNotBeNull();
    [Fact] void should_hold_back_the_small_backlog_while_the_large_one_starts() => _smallBacklogWhileTheLargeOneStarts.ShouldBeNull();
}
