// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelHostComposition.when_deferring_silo_start;

public class and_multiple_orleans_hosted_services_exist : Specification
{
    IServiceCollection _services;
    Exception? _error;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseOrleans(_ => { });
        _services = builder.Services;
        _services.Add(_services.Single(_ => _.ServiceType == typeof(IHostedService) && _.ImplementationType?.FullName == "Orleans.Hosting.SiloHostedService"));
    }

    void Because() => _error = Catch.Exception(_services.UseDeferredSiloStart);

    [Fact] void should_fail_loudly() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
