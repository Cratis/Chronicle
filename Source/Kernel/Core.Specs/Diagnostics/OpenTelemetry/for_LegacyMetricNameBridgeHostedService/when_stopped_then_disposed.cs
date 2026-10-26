// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridgeHostedService;

[Collection("Shared legacy metric bridge")]
public class when_stopped_then_disposed : given.a_started_service
{
    async Task Because()
    {
        await _service.StopAsync(CancellationToken.None);
        _counter.Record(1, _marker);

        using var other = new LegacyMetricNameBridgeHostedService();
        await other.StartAsync(CancellationToken.None);
        _service.Dispose();
        _counter.Record(2, _marker);

        other.Dispose();
        _counter.Record(3, _marker);
    }

    [Fact] void should_release_the_lease_on_stop() => _measurements.Contains(1).ShouldBeFalse();
    [Fact] void should_not_release_another_services_lease_on_disposal() => _measurements.ShouldEqual([2]);
    [Fact] void should_stop_mirroring_when_the_other_service_is_disposed() => _measurements.Contains(3).ShouldBeFalse();
}
