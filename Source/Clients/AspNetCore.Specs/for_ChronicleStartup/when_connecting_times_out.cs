// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Diagnostics;
using Cratis.Chronicle.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleStartup;

public class when_connecting_times_out : given.a_failed_startup
{
    HealthCheckResult _readiness;

    void Establish()
    {
        _failure = new ConnectionTimedOut();
        _connection.Connect().Returns(Task.FromException(_failure));
    }

    async Task Because()
    {
        await _startup.StartAsync(CancellationToken.None);
        _startedSignal = Task.Run(() => _started.CancelAsync());
        await _startedSignal.WaitAsync(TimeSpan.FromSeconds(10));
        await _logs.FailureReported.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var registration = _services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Single(item => item.Name == ChronicleHealthCheck.Name);
        _readiness = await registration.Factory(_services).CheckHealthAsync(new HealthCheckContext { Registration = registration });
    }

    [Fact] void should_report_the_original_failure_once() => _logs.Entries.Count(entry => ReferenceEquals(entry.Error, _failure)).ShouldEqual(1);
    [Fact] void should_report_through_the_shared_startup_category() => _logs.Entries.Single(entry => ReferenceEquals(entry.Error, _failure)).Category.ShouldEqual("Cratis.Chronicle.ChronicleClientStartup");
    [Fact] void should_remain_unhealthy() => _readiness.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_not_stop_the_host() => _lifetime.DidNotReceive().StopApplication();
    [Fact] void should_not_warn_that_use_is_missing() => _logs.Entries.Any(entry => entry.Message.Contains("UseCratisChronicle() was not called")).ShouldBeFalse();
    [Fact] void should_keep_the_header_namespace_warning() => _logs.Entries.Any(entry => entry.Message.Contains("HTTP header")).ShouldBeTrue();
}
