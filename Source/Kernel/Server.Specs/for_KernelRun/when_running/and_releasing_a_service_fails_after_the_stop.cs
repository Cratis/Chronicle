// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.when_running;

public class and_releasing_a_service_fails_after_the_stop : given.a_host
{
    KernelRunResult _result;

    void Establish() => BuildHost(_ => _.AddSingleton<IHostedService, given.ReleaseFailingService>());

    async Task Because() => _result = await Run();

    [Fact] void should_be_a_failed_release() => _result.Outcome.ShouldEqual(KernelRunOutcome.DisposeFailed);
    [Fact] void should_carry_the_failure() => _result.Failure.ShouldBeOfExactType<ObjectDisposedException>();
}
