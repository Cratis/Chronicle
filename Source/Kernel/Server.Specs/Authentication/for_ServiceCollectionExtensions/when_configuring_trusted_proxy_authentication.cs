// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_configuring_trusted_proxy_authentication : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;
    AuthenticationScheme? _scheme;

    async Task Because()
    {
        _services = BuildServices(authenticationEnabled: true, http: new HttpAuthentication { Mode = HttpAuthenticationMode.TrustedProxy });
        _scheme = await _services.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync(TrustedProxyAuthenticationHandler.SchemeName);
    }

    void Destroy() => _services.ServiceProvider.Dispose();

    [Fact] void should_register_the_trusted_proxy_scheme() => _scheme!.HandlerType.ShouldEqual(typeof(TrustedProxyAuthenticationHandler));
    [Fact] void should_register_the_trusted_peers() => _services.ServiceProvider.GetService<TrustedProxyPeers>().ShouldNotBeNull();
}
