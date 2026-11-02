// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_BaselineSelection.when_selecting;

public class and_nothing_narrows_it : Specification
{
    BaselineSelection _result;

    void Because() => _result = BaselineSelection.From(["19.0.0", "19.1.0", "19.4.0", "19.6.1", "19.14.0"], null, null, []);

    [Fact] void should_compare_every_release_in_order() => _result.Baselines.SequenceEqual(["19.0.0", "19.1.0", "19.4.0", "19.6.1", "19.14.0"]).ShouldBeTrue();
    [Fact] void should_exclude_nothing_before_a_floor() => _result.BeforeFloor.ShouldBeEmpty();
    [Fact] void should_exclude_nothing_after_a_ceiling() => _result.AfterCeiling.ShouldBeEmpty();
}
