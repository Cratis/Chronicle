// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.when_running;

public class and_several_services_fail_while_stopping : given.a_host
{
    KernelRunResult _result;

    void Establish() => BuildHost(_ => _
        .AddSingleton<IHostedService, given.StopFailingService>()
        .AddSingleton<IHostedService, given.StopCancellingService>());

    async Task Because() => _result = await Run();

    [Fact] void should_be_a_failed_stop() => _result.Outcome.ShouldEqual(KernelRunOutcome.StopFailed);
    [Fact] void should_carry_every_failure() => ((AggregateException)_result.Failure!).InnerExceptions.Count.ShouldEqual(2);
}
