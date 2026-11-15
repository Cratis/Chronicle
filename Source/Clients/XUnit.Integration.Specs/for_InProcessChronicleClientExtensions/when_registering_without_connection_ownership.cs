// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Microsoft.Extensions.DependencyInjection;

using ConnectionOwnership = KernelCore::Cratis.Chronicle.Clients.ConnectionOwnership;
using IConnectionOwnership = KernelCore::Cratis.Chronicle.Clients.IConnectionOwnership;

namespace Cratis.Chronicle.XUnit.Integration.for_InProcessChronicleClientExtensions;

public class when_registering_without_connection_ownership : Specification
{
    ServiceCollection _services;
    ServiceProvider _provider;
    IConnectionOwnership _ownership;

    void Establish() => _services = new();

    void Because()
    {
        _services.AddInProcessChronicleClient(Substitute.For<IClientArtifactsProvider>(), "specs");
        _provider = _services.BuildServiceProvider();
        _ownership = _provider.GetRequiredService<IConnectionOwnership>();
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_register_connection_ownership() => _ownership.ShouldBeOfExactType<ConnectionOwnership>();
    [Fact]
    void should_share_connection_ownership_between_scopes()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IConnectionOwnership>().ShouldBeSame(_ownership);
    }
}
