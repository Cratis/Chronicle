// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleOrleansInProcessWebApplicationFactory;

public class when_building_the_stock_host : Specification
{
    given.a_build_only_factory _factory;
    IHost _host;
    IAuthorizationPolicyRuntime _runtime;

    void Establish() => _factory = new();

    void Because()
    {
        _host = _factory.BuildHost();
        _runtime = _host.Services.GetRequiredService<IAuthorizationPolicyRuntime>();
    }

    [Fact] void should_use_the_aspnet_policy_runtime() => _runtime.ShouldBeOfExactType<AspNetAuthorizationPolicyRuntime>();
    [Fact] void should_register_the_command_pipeline_only_once() => _factory.PipelineRegistrations.ShouldEqual(1);
    [Fact] void should_preserve_the_fixture_service_provider() => _host.Services.ShouldBeOfExactType<FallbackServiceProvider>();

    void Destroy()
    {
        _host?.Dispose();
        _factory.Dispose();
    }
}
