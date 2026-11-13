// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_RunTiming;

/// <summary>
/// Specifies the absolute duration-plus-settle budget without waiting on a clock.
/// </summary>
public class when_calculating_the_deadline : Specification
{
    TimeSpan _duration;
    TimeSpan _settle;

    void Establish()
    {
        _duration = TimeSpan.FromMinutes(2);
        _settle = TimeSpan.FromMinutes(1);
    }

    [Fact] void should_include_duration_and_settle() => RunTiming.Remaining(_duration, _settle, TimeSpan.Zero).ShouldEqual(TimeSpan.FromMinutes(3));
    [Fact] void should_charge_recovery_time_against_settle() => RunTiming.Remaining(_duration, _settle, TimeSpan.FromSeconds(140)).ShouldEqual(TimeSpan.FromSeconds(40));
    [Fact] void should_expire_at_the_absolute_deadline() => RunTiming.Remaining(_duration, _settle, TimeSpan.FromMinutes(3)).ShouldEqual(TimeSpan.Zero);
    [Fact] void should_not_extend_a_missed_deadline() => RunTiming.Remaining(_duration, _settle, TimeSpan.FromMinutes(4)).ShouldEqual(TimeSpan.Zero);
}
