// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using StorageUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_creating_a_chronicle_user_principal : given.chronicle_authentication_services
{
    ChronicleAuthenticationServices _services;
    ClaimsPrincipal _result;
    void Establish() => _services = BuildServices(authenticationEnabled: true);
    async Task Because() => _result = await _services.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<StorageUser>>()
        .CreateAsync(new StorageUser { Id = Guid.NewGuid(), Username = "operator", SecurityStamp = "stamp" });
    [Fact] void should_mark_users_for_cookies_and_protected_bearer_tickets() => _result.HasClaim(ChronicleClaims.PrincipalKind, ChronicleClaims.User).ShouldBeTrue();
    [Fact] void should_keep_the_identity_user_identifier() => string.IsNullOrWhiteSpace(_result.FindFirstValue(ClaimTypes.NameIdentifier)).ShouldBeFalse();
    void Destroy() => _services.ServiceProvider.Dispose();
}
