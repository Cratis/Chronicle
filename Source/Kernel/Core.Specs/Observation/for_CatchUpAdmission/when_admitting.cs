// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.for_CatchUpAdmission;

public class when_admitting : given.a_catch_up_admission
{
    IDisposable? _belowTheLimit;
    IDisposable? _atTheLimit;
    IDisposable? _alreadyCatchingUp;
    IDisposable? _withoutLimit;
    IDisposable? _whileOthersAreStopped;

    async Task Because()
    {
        RunningJobs(CatchUpOf("one"), CatchUpOf("two"));
        _belowTheLimit = await _admission.TryAdmit(KeyOf("three"), 3, 0);
        _belowTheLimit?.Dispose();

        _atTheLimit = await _admission.TryAdmit(KeyOf("three"), 2, 0);
        _alreadyCatchingUp = await _admission.TryAdmit(KeyOf("one"), 2, 0);
        _withoutLimit = await _admission.TryAdmit(KeyOf("three"), 0, 0);

        RunningJobs(CatchUpOf("one", JobStatus.Stopped), CatchUpOf("two", JobStatus.Stopped));
        _whileOthersAreStopped = await _admission.TryAdmit(KeyOf("three"), 2, 0);
    }

    [Fact] void should_admit_below_the_limit() => _belowTheLimit.ShouldNotBeNull();
    [Fact] void should_hold_back_at_the_limit() => _atTheLimit.ShouldBeNull();
    [Fact] void should_always_admit_an_observer_that_already_has_a_catch_up_job() => _alreadyCatchingUp.ShouldNotBeNull();
    [Fact] void should_not_limit_when_the_limit_is_zero() => _withoutLimit.ShouldNotBeNull();
    [Fact] void should_not_count_stopped_jobs() => _whileOthersAreStopped.ShouldNotBeNull();
}
