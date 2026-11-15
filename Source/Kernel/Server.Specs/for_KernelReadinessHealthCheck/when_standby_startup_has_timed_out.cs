// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelReadinessHealthCheck;

public class when_standby_startup_has_timed_out : Specification
{
    HealthCheckResult _result;

    async Task Because()
    {
        using var started = new CancellationTokenSource();
        using var stopping = new CancellationTokenSource();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Returns(started.Token);
        lifetime.ApplicationStopping.Returns(stopping.Token);
        var options = new ChronicleOptions();
        options.Clustering.SingleWriterLease.Standby = true;
        options.Clustering.SingleWriterLease.StandbyStartupTimeout = TimeSpan.FromMinutes(10);
        var lease = Substitute.For<ISingleWriterLease>();
        lease.IsHeld.Returns(true);
        lease.HeldFor.Returns(TimeSpan.FromMinutes(11));

        _result = await new KernelReadinessHealthCheck(given.startup_state.For(lifetime, options, lease)).CheckHealthAsync(new());
    }

    [Fact] void should_report_unhealthy_so_readiness_fails() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_not_mark_standby() => _result.Data.ContainsKey(SingleWriterLeaseStandby.HealthDataKey).ShouldBeFalse();
    [Fact] void should_name_the_timeout() => _result.Description.ShouldContain(nameof(SingleWriterLease.StandbyStartupTimeout));
}
