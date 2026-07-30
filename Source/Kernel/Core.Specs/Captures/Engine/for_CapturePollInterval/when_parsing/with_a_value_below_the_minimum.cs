// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CapturePollInterval.when_parsing;

public class with_a_value_below_the_minimum : Specification
{
    bool _parsed;
    TimeSpan _interval;

    void Because() => _parsed = CapturePollInterval.TryParse("10s", out _interval);

    [Fact] void should_parse() => _parsed.ShouldBeTrue();
    [Fact] void should_clamp_to_the_minimum() => _interval.ShouldEqual(CapturePollInterval.Minimum);
}
