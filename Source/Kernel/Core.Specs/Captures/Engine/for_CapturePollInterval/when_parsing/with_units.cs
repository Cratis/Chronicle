// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CapturePollInterval.when_parsing;

public class with_units : Specification
{
    [Fact]
    void should_parse_minutes()
    {
        CapturePollInterval.TryParse("5m", out var interval).ShouldBeTrue();
        interval.ShouldEqual(TimeSpan.FromMinutes(5));
    }

    [Fact]
    void should_parse_hours()
    {
        CapturePollInterval.TryParse("2h", out var interval).ShouldBeTrue();
        interval.ShouldEqual(TimeSpan.FromHours(2));
    }

    [Fact]
    void should_parse_days()
    {
        CapturePollInterval.TryParse("1d", out var interval).ShouldBeTrue();
        interval.ShouldEqual(TimeSpan.FromDays(1));
    }

    [Fact]
    void should_parse_bare_number_as_minutes()
    {
        CapturePollInterval.TryParse("10", out var interval).ShouldBeTrue();
        interval.ShouldEqual(TimeSpan.FromMinutes(10));
    }
}
