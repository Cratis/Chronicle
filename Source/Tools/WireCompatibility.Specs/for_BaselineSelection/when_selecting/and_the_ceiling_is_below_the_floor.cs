// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_BaselineSelection.when_selecting;

public class and_the_ceiling_is_below_the_floor : Specification
{
    BaselineSelection _result;

    void Because() => _result = BaselineSelection.From(["19.0.0", "19.1.0", "19.4.0", "19.6.1", "19.14.0"], "19.6.1", "19.4.9", []);

    [Fact] void should_compare_nothing() => _result.Baselines.ShouldBeEmpty();
    [Fact] void should_report_releases_before_the_floor_independently() => _result.BeforeFloor.SequenceEqual(["19.0.0", "19.1.0", "19.4.0"]).ShouldBeTrue();
    [Fact] void should_report_releases_after_the_ceiling_independently() => _result.AfterCeiling.SequenceEqual(["19.6.1", "19.14.0"]).ShouldBeTrue();
}
