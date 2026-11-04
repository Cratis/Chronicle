// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion.when_hosting_the_kernel_in_the_application_container;

public class and_the_kernel_is_added_after_the_client : given.an_application_host_with_the_kernel
{
    protected override bool KernelBeforeClient => false;

    Task Because() => DispatchAKernelCommand();

    [Fact] void should_have_kernel_commands_in_the_type_universe() => _kernelCommandsInTheUniverse.ShouldBeGreaterThan(0);
    [Fact] void should_have_kernel_queries_in_the_type_universe() => _kernelQueriesInTheUniverse.ShouldBeGreaterThan(0);
    [Fact] void should_keep_every_kernel_command() => _servedKernelCommands.Count().ShouldEqual(_kernelCommandsInTheUniverse);
    [Fact] void should_keep_every_kernel_query() => _servedKernelQueries.Count().ShouldEqual(_kernelQueriesInTheUniverse);
    [Fact] void should_find_a_handler_for_the_kernel_command() => _kernelCommandHandlerFound.ShouldBeTrue();
    [Fact] void should_dispatch_the_kernel_command_past_handler_lookup() => _kernelCommandResult.ExceptionMessages.Any(_ => _.StartsWith("No handler found for command", StringComparison.Ordinal)).ShouldBeFalse();
}
