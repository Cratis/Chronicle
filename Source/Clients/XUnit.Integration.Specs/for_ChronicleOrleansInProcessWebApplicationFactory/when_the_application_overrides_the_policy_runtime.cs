// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleOrleansInProcessWebApplicationFactory;

public class when_the_application_overrides_the_policy_runtime : Specification
{
    given.a_build_only_factory _factory;
    IHost _host;
    IAuthorizationPolicyRuntime _override;
    IAuthorizationPolicyRuntime _runtime;

    void Establish()
    {
        _override = Substitute.For<IAuthorizationPolicyRuntime>();
        _factory = new(services => services.AddSingleton(_override));
    }

    void Because()
    {
        _host = _factory.BuildHost();
        _runtime = _host.Services.GetRequiredService<IAuthorizationPolicyRuntime>();
    }

    [Fact] void should_preserve_the_application_runtime() => _runtime.ShouldEqual(_override);

    void Destroy()
    {
        _host?.Dispose();
        _factory.Dispose();
    }
}
