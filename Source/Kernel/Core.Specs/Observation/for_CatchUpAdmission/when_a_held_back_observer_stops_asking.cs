// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.for_CatchUpAdmission;

public class when_a_held_back_observer_stops_asking : given.a_catch_up_admission
{
    IDisposable? _beforeItIsForgotten;
    IDisposable? _afterItIsForgotten;

    async Task Because()
    {
        RunningJobs(CatchUpOf("running"));
        await _admission.TryAdmit(KeyOf("gone"), 1, 5_000);
        await _admission.TryAdmit(KeyOf("waiting"), 1, 10);

        RunningJobs(CatchUpOf("running", JobStatus.CompletedSuccessfully));
        _time.Advance(CatchUpAdmission.WaiterLifetime - TimeSpan.FromSeconds(1));
        _beforeItIsForgotten = await _admission.TryAdmit(KeyOf("waiting"), 1, 10);

        _time.Advance(TimeSpan.FromSeconds(2));
        _afterItIsForgotten = await _admission.TryAdmit(KeyOf("waiting"), 1, 10);
    }

    [Fact] void should_keep_the_place_for_it_until_it_is_forgotten() => _beforeItIsForgotten.ShouldBeNull();
    [Fact] void should_admit_the_next_observer_once_it_is_forgotten() => _afterItIsForgotten.ShouldNotBeNull();
}
