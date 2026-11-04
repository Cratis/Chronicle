// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_registering_kernel_projections_fails_with_a_defect : given.a_startup_task
{
    readonly InvalidOperationException _defect = new("Invalid projection");
    Exception _error;

    void Establish() => _kernelProjections.DiscoverAndRegister(_eventStore).Returns(_ => Task.FromException(_defect));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_propagate_the_defect() => _error.ShouldEqual(_defect);
    [Fact] void should_not_defer() => _kernelProjectionRetries.DidNotReceiveWithAnyArgs().Defer(default!);
}
