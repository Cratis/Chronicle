// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge;

public class when_disposed : given.a_bridge_on_a_private_meter
{
    void Because()
    {
        var counter = _meter.CreateCounter<int>(Convention);
        counter.Add(1);
        _bridge.Dispose();
        counter.Add(1);
    }

    [Fact] void should_only_have_mirrored_before_disposal() => _mirrored.Count.ShouldEqual(1);
}
