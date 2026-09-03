// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_configuring_the_authentication_cookie : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;
    CookieAuthenticationOptions _result;

    void Because()
    {
        _services = BuildServices(authenticationEnabled: true);
        _result = _services.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
    }

    void Destroy() => _services.ServiceProvider.Dispose();

    [Fact] void should_require_a_secure_cookie() => _result.Cookie.SecurePolicy.ShouldEqual(CookieSecurePolicy.Always);
}
