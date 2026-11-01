// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Placement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Metadata;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Jobs.Placement.for_JobStepPlacementRegistration;

public class when_adding_chronicle_to_a_silo : Specification
{
    IServiceCollection _services;
    ServiceDescriptor _strategy;
    ServiceDescriptor _director;

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

        _strategy = _services.Single(_ => _.IsKeyedService && _.ServiceType == typeof(PlacementStrategy) && Equals(_.ServiceKey, nameof(JobStepsPlacementStrategy)));
        _director = _services.Single(_ => _.IsKeyedService && _.ServiceType == typeof(IPlacementDirector) && Equals(_.ServiceKey, typeof(JobStepsPlacementStrategy)));
    }

    [Fact] void should_register_the_strategy_by_its_wire_name() => _strategy.KeyedImplementationType.ShouldEqual(typeof(JobStepsPlacementStrategy));
    [Fact] void should_register_the_worker_director() => _director.KeyedImplementationType.ShouldEqual(typeof(JobWorkerPlacementDirector));
    [Fact] void should_register_the_conditional_properties_provider() => _services.Any(_ => _.ServiceType == typeof(IGrainPropertiesProvider) && _.ImplementationType == typeof(JobStepPlacementProperties)).ShouldBeTrue();
    [Fact] void should_not_register_the_unconditional_properties_provider() => _services.Any(_ => !_.IsKeyedService && _.ImplementationType == typeof(JobStepsPlacementProperties)).ShouldBeFalse();
    [Fact] void should_not_constrain_controllers() => _services.Any(_ => _.IsKeyedService && _.ServiceType == typeof(IPlacementDirector) && Equals(_.ServiceKey, typeof(JobsPlacementStrategy))).ShouldBeFalse();
}
