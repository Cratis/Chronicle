// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.for_CatchUpAdmission;

public class when_observers_are_equally_far_behind : given.a_catch_up_admission
{
    IDisposable? _laterWhenAPlaceIsFree;
    IDisposable? _earlierWhenAPlaceIsFree;

    async Task Because()
    {
        RunningJobs(CatchUpOf("running"));
        await _admission.TryAdmit(KeyOf("earlier"), 1, 100);
        _time.Advance(TimeSpan.FromSeconds(1));
        await _admission.TryAdmit(KeyOf("later"), 1, 100);

        RunningJobs(CatchUpOf("running", JobStatus.CompletedSuccessfully));
        _laterWhenAPlaceIsFree = await _admission.TryAdmit(KeyOf("later"), 1, 100);
        _earlierWhenAPlaceIsFree = await _admission.TryAdmit(KeyOf("earlier"), 1, 100);
    }

    [Fact] void should_keep_the_place_for_the_observer_that_waited_longest() => _laterWhenAPlaceIsFree.ShouldBeNull();
    [Fact] void should_admit_the_observer_that_waited_longest() => _earlierWhenAPlaceIsFree.ShouldNotBeNull();
}
