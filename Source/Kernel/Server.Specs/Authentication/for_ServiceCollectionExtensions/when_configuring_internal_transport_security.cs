// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_configuring_internal_transport_security : given.chronicle_authentication_services
{
    bool _transportSecurityDisabled;

    void Because()
    {
        using var services = BuildServices(authenticationEnabled: true, useInternalAuthority: true).ServiceProvider;
        _transportSecurityDisabled = services.GetRequiredService<IOptions<OpenIddictServerAspNetCoreOptions>>()
            .Value.DisableTransportSecurityRequirement;
    }

    [Fact] void should_require_https_even_when_no_listener_certificate_is_configured() => _transportSecurityDisabled.ShouldBeFalse();
}
