// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelHostedServices;

/// <summary>
/// The guard has to catch grain access that is not in the hosted service's own constructor, or it passes for the
/// cases that actually happen: a cache or service that takes the grain factory, or a factory registration.
/// </summary>
public class when_a_hosted_service_reaches_grains_through_a_dependency : Specification
{
    ServiceCollection _services;
    string[] _offenders;

    void Establish()
    {
        _services = new();
        _services.AddSingleton<IGrainBackedDependency, GrainBackedDependency>();
        _services.AddHostedService<HostedServiceWithAGrainBackedDependency>();
        _services.AddSingleton<IHostedService>(_ => new HostedServiceWithAGrainBackedDependency(new GrainBackedDependency(Substitute.For<IGrainFactory>())));
    }

    void Because() => _offenders = [.. HostedServiceGrainAccess.Offenders(_services)];

    [Fact] void should_find_the_dependency() => _offenders.ShouldContain(typeof(HostedServiceWithAGrainBackedDependency).FullName);
    [Fact] void should_find_the_factory_registration() => _offenders.ShouldContain($"{typeof(when_a_hosted_service_reaches_grains_through_a_dependency).FullName} (factory registration)");

    interface IGrainBackedDependency;

    sealed class GrainBackedDependency(IGrainFactory grains) : IGrainBackedDependency
    {
        public IGrainFactory Grains { get; } = grains;
    }

    sealed class HostedServiceWithAGrainBackedDependency(IGrainBackedDependency dependency) : IHostedService
    {
        public IGrainBackedDependency Dependency { get; } = dependency;

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
