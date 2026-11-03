// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_ProjectionsManager.given;

namespace Cratis.Chronicle.Projections.for_RegistrationTiming;

public class when_marking_phases : Specification
{
    RegistrationTiming _timing;

    void Establish()
    {
        var clock = new AdvancingClock();
        _timing = new RegistrationTiming(clock);
        clock.Advance(TimeSpan.FromSeconds(2));
        _timing.Mark(RegistrationPhase.Detecting);
        clock.Advance(TimeSpan.FromSeconds(3));
        _timing.Mark(RegistrationPhase.Engine);
        clock.Advance(TimeSpan.FromSeconds(15));
        _timing.Mark(RegistrationPhase.Subscribing);
        clock.Advance(TimeSpan.FromSeconds(1));
        _timing.Complete();
    }

    [Fact] void should_attribute_the_time_since_the_start_to_the_first_phase() => _timing.Detecting.ShouldEqual(TimeSpan.FromSeconds(2));
    [Fact] void should_attribute_the_time_since_the_previous_mark_to_each_later_phase() => _timing.Engine.ShouldEqual(TimeSpan.FromSeconds(3));
    [Fact] void should_measure_the_subscribe_phase() => _timing.Subscribing.ShouldEqual(TimeSpan.FromSeconds(15));
    [Fact] void should_report_zero_for_a_phase_that_never_ran() => _timing.Retiring.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_include_the_time_after_the_last_mark_in_the_total() => _timing.Total.ShouldEqual(TimeSpan.FromSeconds(21));
}
