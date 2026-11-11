// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.when_running;

public class and_a_stop_is_requested_during_startup : given.a_host
{
    KernelRunResult _result;

    void Establish() => BuildHost(_ => _.AddSingleton<IHostedService>(new given.StopRequestingStartService(_stopRequest)));

    async Task Because() => _result = await Run();

    [Fact] void should_be_a_stop_during_startup() => _result.Outcome.ShouldEqual(KernelRunOutcome.StoppedDuringStartup);
}
