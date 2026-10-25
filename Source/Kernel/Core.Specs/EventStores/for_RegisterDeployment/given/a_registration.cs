// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventStores.for_RegisterDeployment.given;

public class a_registration : Specification
{
    protected readonly CommandScenario<RegisterDeployment> _scenario = ChronicleCommandScenario.For<RegisterDeployment>();
    protected IEventStoreDeploymentRegistry _registry;
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected CommandResult _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _storage.HasEventStore("the-store").Returns(true);
        _grainFactory = Substitute.For<IGrainFactory>();
        _registry = Substitute.For<IEventStoreDeploymentRegistry>();
        _registry.Register(10UL, "fingerprint").Returns(new DeploymentDecision(true, DeploymentState.Empty, new(10UL, "fingerprint")));
        _grainFactory.GetGrain<IEventStoreDeploymentRegistry>("the-store").Returns(_registry);
        _scenario.Services.AddSingleton(_storage);
        _scenario.Services.AddSingleton(_grainFactory);
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
