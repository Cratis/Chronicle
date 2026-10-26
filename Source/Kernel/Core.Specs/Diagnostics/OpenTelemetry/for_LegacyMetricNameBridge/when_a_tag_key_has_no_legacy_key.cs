// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge;

public class when_a_tag_key_has_no_legacy_key : given.a_bridge_on_a_private_meter
{
    void Because() => _meter.CreateCounter<int>(Convention).Add(1, new KeyValuePair<string, object?>("something.else", "value"));

    [Fact] void should_pass_the_key_through_unchanged() => _mirrored[0].Tags[0].Key.ShouldEqual("something.else");
    [Fact] void should_keep_the_value() => _mirrored[0].Tags[0].Value.ShouldEqual("value");
}
