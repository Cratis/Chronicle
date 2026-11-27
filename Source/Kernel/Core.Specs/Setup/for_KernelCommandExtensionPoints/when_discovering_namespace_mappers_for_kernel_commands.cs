// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints;

public class when_discovering_namespace_mappers_for_kernel_commands : given.a_host_shared_with_an_application
{
    ICommandExceptionMapper[] _mappers;

    void Because()
    {
        using var kernel = KernelCommandExecution.Begin();
        _mappers = _provider.GetRequiredService<IInstancesOf<ICommandExceptionMapper>>().ToArray();
    }

    [Fact] void should_discover_the_namespace_mapper() => _mappers.OfType<NamespaceCommandExceptionMapper>().Count().ShouldEqual(1);
    [Fact] void should_exclude_the_application_mapper() => _mappers.OfType<given.ApplicationCommandExceptionMapper>().ShouldBeEmpty();
}
