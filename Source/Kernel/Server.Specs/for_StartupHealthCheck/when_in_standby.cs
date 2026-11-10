// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_StartupHealthCheck;

public class when_in_standby : Specification
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
        var lease = Substitute.For<ISingleWriterLease>();
        lease.IsWaiting.Returns(true);

        _result = await new StartupHealthCheck(given.startup_state.For(lifetime, options, lease)).CheckHealthAsync(new HealthCheckContext());
    }

    [Fact] void should_report_degraded_so_readiness_answers_ok() => _result.Status.ShouldEqual(HealthStatus.Degraded);
    [Fact] void should_mark_standby() => _result.Data.ContainsKey(SingleWriterLeaseStandby.HealthDataKey).ShouldBeTrue();
}
