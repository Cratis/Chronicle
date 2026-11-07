// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Microsoft.Extensions.DependencyInjection;

using IConnectionOwnership = KernelCore::Cratis.Chronicle.Clients.IConnectionOwnership;

namespace Cratis.Chronicle.XUnit.Integration.for_InProcessChronicleClientExtensions;

public class when_registering_with_silo_connection_ownership : Specification
{
    ServiceCollection _services;
    ServiceProvider _provider;
    IConnectionOwnership _siloOwnership;

    void Establish()
    {
        _siloOwnership = Substitute.For<IConnectionOwnership>();
        _services = new();
        _services.AddSingleton(_siloOwnership);
    }

    void Because()
    {
        _services.AddInProcessChronicleClient(Substitute.For<IClientArtifactsProvider>(), "specs");
        _provider = _services.BuildServiceProvider();
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_use_the_silos_connection_ownership() => _provider.GetRequiredService<IConnectionOwnership>().ShouldBeSame(_siloOwnership);
    [Fact] void should_register_only_one_connection_ownership() => _provider.GetServices<IConnectionOwnership>().ShouldContainOnly(_siloOwnership);
}
