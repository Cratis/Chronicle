// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge;

public class when_recording_a_gauge : given.a_bridge_on_a_private_meter
{
    void Because() => _meter.CreateGauge<long>(ConventionGauge).Record(42, new KeyValuePair<string, object?>("cratis.event_sequence.id", "log"));

    [Fact] void should_mirror_once() => _mirrored.Count.ShouldEqual(1);
    [Fact] void should_use_the_legacy_name() => _mirrored[0].Name.ShouldEqual("legacy-level");
    [Fact] void should_keep_the_value() => _mirrored[0].Value.ShouldEqual(42);
    [Fact] void should_translate_the_tag_key() => _mirrored[0].Tags[0].Key.ShouldEqual("EventSequenceId");
}
