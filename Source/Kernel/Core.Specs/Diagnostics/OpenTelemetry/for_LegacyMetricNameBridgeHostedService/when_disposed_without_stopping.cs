// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridgeHostedService;

[Collection("Shared legacy metric bridge")]
public class when_disposed_without_stopping : given.a_started_service
{
    void Because()
    {
        _counter.Record(1, _marker);
        _service.Dispose();
        _counter.Record(2, _marker);
    }

    [Fact] void should_mirror_before_disposal() => _measurements.ShouldEqual([1]);
    [Fact] void should_release_the_last_lease_without_stop_async() => _measurements.Contains(2).ShouldBeFalse();
}
