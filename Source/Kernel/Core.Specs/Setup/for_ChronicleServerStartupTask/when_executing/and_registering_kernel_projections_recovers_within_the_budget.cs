// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_registering_kernel_projections_recovers_within_the_budget : given.a_startup_task
{
    Exception _error;

    void Establish() => _kernelProjections.DiscoverAndRegister(_eventStore).Returns(
        _ => Task.FromException(new TimeoutException()),
        _ => Task.CompletedTask);

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] async Task should_retry_the_registration() => await _kernelProjections.Received(2).DiscoverAndRegister(_eventStore);
    [Fact] void should_not_defer() => _kernelProjectionRetries.DidNotReceiveWithAnyArgs().Defer(default!);
}
