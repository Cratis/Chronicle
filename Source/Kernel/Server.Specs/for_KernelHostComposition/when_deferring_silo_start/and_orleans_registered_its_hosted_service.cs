// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelHostComposition.when_deferring_silo_start;

public class and_orleans_registered_its_hosted_service : Specification
{
    IServiceCollection _services;
    int _index;
    ServiceDescriptor _original;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseOrleans(_ => { });
        _services = builder.Services;
        _original = _services.Single(_ => _.ServiceType == typeof(IHostedService) && _.ImplementationType?.FullName == "Orleans.Hosting.SiloHostedService");
        _index = _services.IndexOf(_original);
    }

    void Because() => _services.UseDeferredSiloStart();

    [Fact] void should_remove_the_original() => _services.Contains(_original).ShouldBeFalse();
    [Fact] void should_keep_the_hosted_service_position() => _services[_index].ServiceType.ShouldEqual(typeof(IHostedService));
    [Fact] void should_replace_it_with_a_factory() => _services[_index].ImplementationFactory.ShouldNotBeNull();
}
