// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cluster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Jobs.Placement.for_JobWorkerPlacementStrategy;

public class when_registering_with_chronicle : Specification
{
    IServiceCollection _services;
    ServiceDescriptor _strategy;
    ServiceDescriptor _director;
    ServiceDescriptor _eligibility;

    void Because()
    {
        new HostBuilder()
            .UseDefaultServiceProvider(options => options.ValidateOnBuild = false)
            .UseOrleans(silo =>
            {
                silo.AddChronicleToSilo();
                _services = silo.Services;
            })
            .Build()
            .Dispose();

        _strategy = _services.Single(_ => _.IsKeyedService && _.ServiceType == typeof(PlacementStrategy) && Equals(_.ServiceKey, nameof(JobWorkerPlacementStrategy)));
        _director = _services.Single(_ => _.IsKeyedService && _.ServiceType == typeof(IPlacementDirector) && Equals(_.ServiceKey, typeof(JobWorkerPlacementStrategy)));
        _eligibility = _services.Single(_ => _.ServiceType == typeof(IRoleEligibility));
    }

    [Fact] void should_register_the_strategy_by_its_wire_name() => _strategy.KeyedImplementationType.ShouldEqual(typeof(JobWorkerPlacementStrategy));
    [Fact] void should_register_the_complete_director() => _director.KeyedImplementationType.ShouldEqual(typeof(JobWorkerPlacementDirector));
    [Fact] void should_register_role_eligibility_as_a_singleton() => _eligibility.Lifetime.ShouldEqual(ServiceLifetime.Singleton);
}
