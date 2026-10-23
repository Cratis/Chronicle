// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelReadinessHealthCheck;

public class when_ready : Specification
{
    HealthCheckResult _result;

    async Task Because()
    {
        using var started = new CancellationTokenSource();
        using var stopping = new CancellationTokenSource();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Returns(started.Token);
        lifetime.ApplicationStopping.Returns(stopping.Token);
        await started.CancelAsync();
        _result = await new KernelReadinessHealthCheck(new(lifetime)).CheckHealthAsync(new());
    }

    [Fact] void should_report_healthy() => _result.Status.ShouldEqual(HealthStatus.Healthy);
}
