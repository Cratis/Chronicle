// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_KernelHostComposition.when_adding_the_storage_authorization_health_check;

public class and_services_are_built : Specification
{
    HealthCheckRegistration _registration;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddStorageAuthorizationHealthCheck();
        using var provider = services.BuildServiceProvider();
        _registration = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Single();
    }

    [Fact] void should_register_it_as_storage_authorization() => _registration.Name.ShouldEqual("storage-authorization");
    [Fact] void should_tag_it_ready() => _registration.Tags.ShouldContain("ready");
}
