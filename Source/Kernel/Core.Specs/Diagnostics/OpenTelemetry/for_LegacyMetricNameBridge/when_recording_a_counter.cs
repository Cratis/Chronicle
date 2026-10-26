// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge;

public class when_recording_a_counter : given.a_bridge_on_a_private_meter
{
    void Because() => _meter.CreateCounter<int>(Convention).Add(
        3,
        new KeyValuePair<string, object?>("cratis.event_store.name", "store"),
        new KeyValuePair<string, object?>("cratis.observer.id", "observer"));

    [Fact] void should_mirror_once() => _mirrored.Count.ShouldEqual(1);
    [Fact] void should_use_the_legacy_name() => _mirrored[0].Name.ShouldEqual("legacy-counter");
    [Fact] void should_use_the_legacy_description() => _mirrored[0].Description.ShouldEqual("A legacy counter");
    [Fact] void should_keep_the_value() => _mirrored[0].Value.ShouldEqual(3);
    [Fact] void should_have_no_unit() => _mirrored[0].Unit.ShouldBeNull();
    [Fact] void should_translate_the_tag_keys() => string.Join(";", _mirrored[0].Tags.Select(_ => $"{_.Key}={_.Value}")).ShouldEqual("EventStore=store;ObserverId=observer");
}
