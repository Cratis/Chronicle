// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints;

public class when_the_kernel_resolves_the_command_filters : given.a_host_shared_with_an_application
{
    ICommandFilter[] _filters;

    void Because()
    {
        using var execution = KernelCommandExecution.Begin();
        _filters = [.. _provider.GetRequiredService<IInstancesOf<ICommandFilter>>()];
    }

    [Fact] void should_include_the_system_store_authorization_filter_once() => _filters.OfType<SystemStoreAuthorizationCommandFilter>().Count().ShouldEqual(1);
    [Fact] void should_include_the_system_store_authorization_filter_as_an_authorization_filter() => _filters.OfType<IAuthorizationCommandFilter>().OfType<SystemStoreAuthorizationCommandFilter>().Count().ShouldEqual(1);
    [Fact] void should_include_the_routing_value_filter_once() => _filters.OfType<SystemStoreCommandFilter>().Count().ShouldEqual(1);
    [Fact] void should_not_include_the_application_command_filter() => _filters.OfType<given.ApplicationCommandFilter>().ShouldBeEmpty();
}
