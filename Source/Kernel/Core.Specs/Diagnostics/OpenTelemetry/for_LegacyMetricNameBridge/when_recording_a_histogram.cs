// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge;

public class when_recording_a_histogram : given.a_bridge_on_a_private_meter
{
    void Because() => _meter.CreateHistogram<double>(ConventionHistogram, "s").Record(1.5);

    [Fact] void should_mirror_once() => _mirrored.Count.ShouldEqual(1);
    [Fact] void should_use_the_legacy_name() => _mirrored[0].Name.ShouldEqual("legacy-duration");
    [Fact] void should_use_the_legacy_unit() => _mirrored[0].Unit.ShouldEqual("s");
    [Fact] void should_keep_the_value() => _mirrored[0].Value.ShouldEqual(1.5);
}
