// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Kernel projection registration already has a background retry, so a busy sibling defers it rather than holding
/// startup until the sibling has drained.
/// </summary>
public class and_registering_kernel_projections_times_out_against_a_busy_sibling : given.a_startup_task
{
    Exception _error;

    void Establish()
    {
        _busySilos.FindBusySilo(Arg.Any<Exception>()).Returns(new BusySilo("10.0.0.2:11111:7", false));
        _kernelProjections.DiscoverAndRegister(_eventStore).Returns(_ => Task.FromException(new TimeoutException("Response did not arrive on time")));
    }

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] async Task should_use_the_startup_retry_budget() => await _kernelProjections.Received(5).DiscoverAndRegister(_eventStore);
    [Fact] void should_defer_to_the_background_retry() => _kernelProjectionRetries.Received(1).Defer(_eventStore);
}
