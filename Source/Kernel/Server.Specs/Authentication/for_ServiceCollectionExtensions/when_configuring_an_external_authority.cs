// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_configuring_an_external_authority : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;
    JwtBearerOptions _result;

    void Because()
    {
        _services = BuildServices(authenticationEnabled: true);
        _result = _services.ServiceProvider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
    }

    void Destroy() => _services.ServiceProvider.Dispose();

    [Fact] void should_use_the_configured_authority() => _result.Authority.ShouldEqual("https://identity.example");
    [Fact] void should_require_the_chronicle_audience() => _result.Audience.ShouldEqual("chronicle");
    [Fact] void should_require_https_metadata() => _result.RequireHttpsMetadata.ShouldBeTrue();
}
