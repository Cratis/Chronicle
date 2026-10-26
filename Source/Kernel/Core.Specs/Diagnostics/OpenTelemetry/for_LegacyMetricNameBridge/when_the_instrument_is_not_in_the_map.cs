// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge;

public class when_the_instrument_is_not_in_the_map : given.a_bridge_on_a_private_meter
{
    void Because() => _meter.CreateCounter<int>(NotMapped).Add(1);

    [Fact] void should_not_mirror() => _mirrored.ShouldBeEmpty();
}
