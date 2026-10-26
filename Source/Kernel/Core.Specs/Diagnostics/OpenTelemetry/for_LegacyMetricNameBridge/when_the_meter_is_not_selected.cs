// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge;

public class when_the_meter_is_not_selected : given.a_bridge_on_a_private_meter
{
    void Because() => _otherMeter.CreateCounter<int>(Convention).Add(1);

    [Fact] void should_not_mirror() => _mirrored.ShouldBeEmpty();
}
