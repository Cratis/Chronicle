// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_the_external_administrator_claim_is_malformed : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;
    ExternalAdministratorClaimValidation[] _validators;
    ValidateOptionsResult _result;

    void Because()
    {
        _services = BuildServices(authenticationEnabled: true);
        _validators = [.. _services.ServiceProvider.GetServices<IValidateOptions<ChronicleOptions>>().OfType<ExternalAdministratorClaimValidation>()];
        var options = new ChronicleOptions
        {
            Authentication = new Configuration.Authentication
            {
                Authority = "https://identity.example",
                ExternalAdministratorClaim = new ExternalAdministratorClaim { Type = "roles" }
            }
        };
        _result = _validators.Single().Validate(null, options);
    }

    void Destroy() => _services.ServiceProvider.Dispose();

    [Fact] void should_register_the_validator_once() => _validators.Length.ShouldEqual(1);
    [Fact] void should_fail() => _result.Failed.ShouldBeTrue();
}
