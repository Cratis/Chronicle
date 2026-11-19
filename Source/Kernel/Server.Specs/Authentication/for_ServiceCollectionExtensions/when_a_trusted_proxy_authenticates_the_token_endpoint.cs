// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_a_trusted_proxy_authenticates_the_token_endpoint : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;
    OpenIddictServerOptions _options;

    void Because()
    {
        _services = BuildServices(
            authenticationEnabled: true,
            useInternalAuthority: true,
            http: new HttpAuthentication { Mode = HttpAuthenticationMode.TrustedProxy });
        _options = _services.ServiceProvider.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
    }

    void Destroy() => _services.ServiceProvider.Dispose();

    [Fact] void should_keep_issuing_tokens_to_clients() => _options.GrantTypes.ShouldContain(OpenIddictConstants.GrantTypes.ClientCredentials);
    [Fact] void should_not_exchange_user_passwords_for_tokens() => _options.GrantTypes.ShouldNotContain(OpenIddictConstants.GrantTypes.Password);
}
