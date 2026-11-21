// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.when_running;

public class and_a_service_overruns_the_shutdown_timeout : given.a_host
{
    KernelRunResult _result;
    readonly given.a_manual_time_provider _timeProvider = new();
    readonly given.StopOverrunningService _service = new();

    void Establish() => BuildHost(_ => _.AddSingleton<IHostedService>(_service), _shutdownTimeout);

    async Task Because()
    {
        var run = KernelRun.RunAsync(_host, _stopRequest.Token, _timeProvider);
        await _service.Stopping.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        _timeProvider.Expire();
        _service.Complete();
        _result = await run.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
    }

    [Fact] void should_be_a_timed_out_stop() => _result.Outcome.ShouldEqual(KernelRunOutcome.StopTimedOut);
    [Fact] void should_not_have_a_failure() => _result.Failure.ShouldBeNull();
    [Fact] void should_report_the_shutdown_timeout() => _result.ShutdownTimeout.ShouldEqual(_shutdownTimeout);
    [Fact] void should_report_a_stop_at_least_as_long_as_the_timeout() => (_result.StopDuration >= _shutdownTimeout).ShouldBeTrue();
}
