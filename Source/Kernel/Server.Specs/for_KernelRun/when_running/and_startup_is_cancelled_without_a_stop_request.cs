// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.when_running;

public class and_startup_is_cancelled_without_a_stop_request : given.a_host
{
    Exception _error;

    void Establish() => BuildHost(_ => _.AddSingleton<IHostedService>(new given.StartFailingService(new TaskCanceledException("A client call timed out"))));

    async Task Because() => _error = await Catch.Exception(Run);

    [Fact] void should_rethrow_the_failure() => _error.ShouldBeOfExactType<TaskCanceledException>();
}
