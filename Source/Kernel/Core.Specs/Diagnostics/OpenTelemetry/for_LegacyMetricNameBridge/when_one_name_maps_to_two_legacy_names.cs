// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge;

public class when_one_name_maps_to_two_legacy_names : given.a_bridge_on_a_private_meter
{
    void Because() => _meter.CreateCounter<int>(ConventionTwoLegacy).Add(2);

    [Fact] void should_mirror_to_both() => _mirrored.Select(_ => _.Name).OrderBy(_ => _).ShouldEqual(["legacy-one", "legacy-two"]);
    [Fact] void should_keep_the_value_on_both() => _mirrored.All(_ => _.Value == 2).ShouldBeTrue();
}
