// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion.when_starting_an_application_host;

public class and_chronicle_is_registered_before_arc : given.a_startable_application_host
{
    protected override bool ChronicleBeforeArc => true;

    Task Because() => StartHost();

    [Fact] void should_preserve_application_commands() => _servedCommandHandlers.Select(_ => _.CommandType).ShouldContain(typeof(given.ApplicationCommand));
    [Fact] void should_preserve_application_queries() => _servedQueryPerformers.Select(_ => _.Type).ShouldContain(typeof(given.ApplicationQuery));

    [Fact] void should_find_application_commands_by_lookup() => _applicationCommandFound.ShouldBeTrue();
    [Fact] void should_find_application_queries_by_lookup() => _applicationQueryFound.ShouldBeTrue();

    [Fact] void should_pass_startup_authorization_validation() => _startupFailure.ShouldBeNull();
    [Fact] void should_have_kernel_commands_in_the_type_universe() => KernelCommandHandlersIn(_allCommandHandlers).ShouldNotBeEmpty();
    [Fact] void should_have_kernel_queries_in_the_type_universe() => KernelQueryPerformersIn(_allQueryPerformers).ShouldNotBeEmpty();
    [Fact] void should_not_serve_any_kernel_command() => KernelCommandHandlersIn(_servedCommandHandlers).ShouldBeEmpty();
    [Fact] void should_not_serve_any_kernel_query() => KernelQueryPerformersIn(_servedQueryPerformers).ShouldBeEmpty();
    [Fact] void should_not_find_a_kernel_command_by_lookup() => _kernelCommandLookups.ShouldNotContain(true);
    [Fact] void should_not_find_a_kernel_query_by_lookup() => _kernelQueryLookups.ShouldNotContain(true);
}
