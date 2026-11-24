// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server.for_KernelHealthResponseWriter;

public class when_writing_the_body : Specification
{
    static readonly HealthReportEntry _healthy = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null);
    static readonly HealthReportEntry _overloaded = new(HealthStatus.Degraded, "The kernel is overloaded: The oldest call to IProjectionsManager has been waiting 40 s", TimeSpan.Zero, null, null);
    static readonly HealthReportEntry _otherDegraded = new(HealthStatus.Degraded, "storage refused", TimeSpan.Zero, null, null);

    string _healthyBody;
    string _overloadedBody;
    string _otherBody;

    void Because()
    {
        _healthyBody = KernelHealthResponseWriter.BodyOf(Report(("kernel-overload", _healthy)));
        _overloadedBody = KernelHealthResponseWriter.BodyOf(Report(("kernel-overload", _overloaded)));
        _otherBody = KernelHealthResponseWriter.BodyOf(Report(("kernel-overload", _healthy), ("storage-authorization", _otherDegraded)));
    }

    [Fact] void should_only_say_healthy_when_nothing_is_wrong() => _healthyBody.ShouldEqual("Healthy");
    [Fact] void should_say_degraded_and_why_when_overloaded() => _overloadedBody.ShouldEqual("Degraded\nThe kernel is overloaded: The oldest call to IProjectionsManager has been waiting 40 s");
    [Fact] void should_leave_other_degraded_reports_as_they_were() => _otherBody.ShouldEqual("Degraded");

    static HealthReport Report(params (string Name, HealthReportEntry Entry)[] entries) =>
        new(entries.ToDictionary(_ => _.Name, _ => _.Entry), TimeSpan.Zero);
}
