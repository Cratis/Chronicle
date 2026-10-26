// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge;

public class when_the_instrument_exists_before_the_bridge_starts : given.a_bridge_on_a_private_meter
{
    void Because()
    {
        _bridge.Dispose();
        _mirrored.Clear();
        var counter = _meter.CreateCounter<int>(Convention);
        _bridge = new LegacyMetricNameBridge(meter => ReferenceEquals(meter, _meter), new Dictionary<string, LegacyMetric[]> { [Convention] = [new("legacy-counter", null, null)] });
        counter.Add(5);
    }

    [Fact] void should_mirror_the_measurement() => _mirrored.Select(_ => _.Value).ShouldEqual([5d]);
}
