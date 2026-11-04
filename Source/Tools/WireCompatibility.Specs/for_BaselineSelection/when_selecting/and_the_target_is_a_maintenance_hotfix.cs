// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_BaselineSelection.when_selecting;

public class and_the_target_is_a_maintenance_hotfix : Specification
{
    BaselineSelection _result;

    void Because() => _result = BaselineSelection.From(["19.0.0", "19.1.0", "19.4.0", "19.6.1", "19.14.0"], null, "19.4.9-hotfix.1", []);

    [Fact] void should_compare_only_releases_on_or_before_the_line() => _result.Baselines.SequenceEqual(["19.0.0", "19.1.0", "19.4.0"]).ShouldBeTrue();
    [Fact] void should_report_later_releases() => _result.AfterCeiling.SequenceEqual(["19.6.1", "19.14.0"]).ShouldBeTrue();
}
