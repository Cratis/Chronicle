// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_KernelHostComposition.when_registering_health_checks;

public class and_registering_kernel_projection_readiness : Specification
{
    HealthCheckRegistration _registration;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseOrleans(_ => { });
        builder.Services.UseDeferredSiloStart();
        using var provider = builder.Services.BuildServiceProvider();
        _registration = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .Single(_ => _.Name == "kernel-projections-registration");
    }

    [Fact] void should_keep_degraded_pods_in_service() => new HealthCheckOptions().ResultStatusCodes[HealthStatus.Degraded].ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_include_it_in_readiness() => _registration.Tags.ShouldContain("ready");
    [Fact] void should_exclude_it_from_liveness() => _registration.Tags.ShouldNotContain("live");
    [Fact] void should_create_the_registration_check()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IKernelProjectionRegistrationRetries>());
        using var provider = services.BuildServiceProvider();
        _registration.Factory(provider).ShouldBeOfExactType<KernelProjectionsRegistrationHealthCheck>();
    }
}
