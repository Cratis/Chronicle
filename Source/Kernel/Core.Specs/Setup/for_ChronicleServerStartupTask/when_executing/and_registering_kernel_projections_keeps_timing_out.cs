// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_registering_kernel_projections_keeps_timing_out : given.a_startup_task
{
    Exception _error;

    void Establish() => _kernelProjections.DiscoverAndRegister(_eventStore).Returns(_ => Task.FromException(new TimeoutException()));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] async Task should_use_the_startup_retry_budget() => await _kernelProjections.Received(5).DiscoverAndRegister(_eventStore);
    [Fact] void should_defer_once() => _kernelProjectionRetries.Received(1).Defer(_eventStore);
    [Fact] async Task should_register_persisted_definitions() => await _projectionsServiceClient.Received(1).Register(_eventStore, Arg.Any<IEnumerable<ProjectionDefinition>>());
    [Fact] void should_ensure_bootstrap_clients() => _bootstrapClientsEnsured.ShouldBeTrue();
    [Fact] void should_not_report_a_terminal_failure() => LogMessages.Any(_ => _.Contains("still could not reach a sibling silo")).ShouldBeFalse();
    [Fact] void should_preserve_startup_backoff() => RequestedDelays.ShouldEqual(new[] { 2, 4, 8, 16 }.Select(_ => TimeSpan.FromSeconds(_)));
}
