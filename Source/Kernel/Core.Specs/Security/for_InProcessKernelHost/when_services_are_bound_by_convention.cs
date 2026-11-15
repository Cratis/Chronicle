// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_InProcessKernelHost;

public class when_services_are_bound_by_convention
{
    [Fact]
    public void should_not_trust_a_server_but_should_trust_explicit_in_process_services()
    {
        var services = new ServiceCollection();
        services.AddTypeDiscovery();
        services.AddBindingsByConvention();
        services.AddSelfBindings();
        using (var server = services.BuildServiceProvider())
        {
            server.GetService<InProcessKernelHost>().ShouldBeNull();
        }

        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(services);
        silo.AddChronicleServicesAsInMemory();
        using var inProcess = services.BuildServiceProvider();
        inProcess.GetService<InProcessKernelHost>().ShouldNotBeNull();
    }
}
