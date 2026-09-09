// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Validation;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_configuring_internal_token_validation : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;
    OpenIddictValidationOptions _result;

    void Because()
    {
        _services = BuildServices(authenticationEnabled: true, useInternalAuthority: true);
        _result = _services.ServiceProvider
            .GetRequiredService<IOptionsMonitor<OpenIddictValidationOptions>>()
            .CurrentValue;
    }

    void Destroy() => _services.ServiceProvider.Dispose();

    [Fact] void should_validate_the_audience() => _result.TokenValidationParameters.ValidateAudience.ShouldBeTrue();
    [Fact] void should_require_the_chronicle_audience() => _result.TokenValidationParameters.ValidAudience.ShouldEqual(WellKnownAudiences.Chronicle);
}
