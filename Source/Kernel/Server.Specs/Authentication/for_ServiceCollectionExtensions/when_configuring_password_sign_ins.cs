// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_configuring_password_sign_ins : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;
    int _minimum;
    void Establish() => _services = BuildServices(authenticationEnabled: true);
    void Because() => _minimum = _services.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value.Password.RequiredLength;
    [Fact] void should_share_the_command_password_minimum() => _minimum.ShouldEqual(PasswordPolicy.MinimumLength);
    [Fact] void should_require_at_least_eight_characters() => _minimum.ShouldEqual(8);
    void Destroy() => _services.ServiceProvider.Dispose();
}
