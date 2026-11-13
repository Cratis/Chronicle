// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server.for_KernelHealthResponseWriter;

public class when_writing_status : Specification
{
    static readonly HealthReportEntry _healthy = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null);
    static readonly HealthReportEntry _degraded = new(HealthStatus.Degraded, "storage refused", TimeSpan.Zero, null, null);
    static readonly HealthReportEntry _standby = new(HealthStatus.Degraded, "standby", TimeSpan.Zero, null, SingleWriterLeaseStandby.Result("standby").Data);
    static readonly HealthReportEntry _unhealthy = new(HealthStatus.Unhealthy, "down", TimeSpan.Zero, null, null);

    string _healthyStatus;
    string _degradedStatus;
    string _standbyStatus;
    string _standbyButUnhealthyStatus;

    void Because()
    {
        _healthyStatus = KernelHealthResponseWriter.StatusOf(Report(_healthy));
        _degradedStatus = KernelHealthResponseWriter.StatusOf(Report(_healthy, _degraded));
        _standbyStatus = KernelHealthResponseWriter.StatusOf(Report(_healthy, _standby));
        _standbyButUnhealthyStatus = KernelHealthResponseWriter.StatusOf(Report(_standby, _unhealthy));
    }

    [Fact] void should_keep_healthy() => _healthyStatus.ShouldEqual("Healthy");
    [Fact] void should_keep_degraded_without_standby() => _degradedStatus.ShouldEqual("Degraded");
    [Fact] void should_name_standby() => _standbyStatus.ShouldEqual(SingleWriterLeaseStandby.Status);
    [Fact] void should_report_unhealthy_over_standby() => _standbyButUnhealthyStatus.ShouldEqual("Unhealthy");

    static HealthReport Report(params HealthReportEntry[] entries) =>
        new(entries.Select((entry, index) => (entry, index)).ToDictionary(_ => $"check-{_.index}", _ => _.entry), TimeSpan.Zero);
}
