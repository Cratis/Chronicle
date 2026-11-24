// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.Load;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_KernelHostComposition.when_registering_health_checks;

public class and_registering_kernel_overload_readiness : Specification
{
    HealthCheckRegistration _registration;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseOrleans(_ => { });
        builder.Services.UseDeferredSiloStart();
        using var provider = builder.Services.BuildServiceProvider();
        _registration = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .Single(_ => _.Name == "kernel-overload");
    }

    [Fact] void should_include_it_in_readiness() => _registration.Tags.ShouldContain("ready");
    [Fact] void should_exclude_it_from_liveness() => _registration.Tags.ShouldNotContain("live");
    [Fact] void should_create_the_overload_check()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IKernelLoad>());
        using var provider = services.BuildServiceProvider();
        _registration.Factory(provider).ShouldBeOfExactType<KernelOverloadHealthCheck>();
    }
}
