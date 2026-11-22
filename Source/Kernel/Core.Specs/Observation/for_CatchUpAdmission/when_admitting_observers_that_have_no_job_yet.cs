// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_CatchUpAdmission;

/// <summary>
/// An observer that was admitted holds its place until it has started its job, which is not in the job storage yet.
/// </summary>
public class when_admitting_observers_that_have_no_job_yet : given.a_catch_up_admission
{
    IDisposable? _first;
    IDisposable? _second;
    IDisposable? _third;
    IDisposable? _thirdAfterTheFirstStarted;

    async Task Because()
    {
        _first = await _admission.TryAdmit(KeyOf("first"), 2);
        _second = await _admission.TryAdmit(KeyOf("second"), 2);
        _third = await _admission.TryAdmit(KeyOf("third"), 2);
        _first?.Dispose();
        _thirdAfterTheFirstStarted = await _admission.TryAdmit(KeyOf("third"), 2);
    }

    [Fact] void should_admit_the_first_up_to_the_limit() => _first.ShouldNotBeNull();
    [Fact] void should_admit_the_second_up_to_the_limit() => _second.ShouldNotBeNull();
    [Fact] void should_hold_back_the_third_while_the_others_are_starting() => _third.ShouldBeNull();
    [Fact] void should_admit_the_third_once_a_place_is_released() => _thirdAfterTheFirstStarted.ShouldNotBeNull();
}
