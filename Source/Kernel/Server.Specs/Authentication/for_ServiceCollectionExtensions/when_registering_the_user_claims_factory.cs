// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using StorageUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_registering_the_user_claims_factory : given.chronicle_authentication_services
{
    IServiceCollection _services;

    async Task Because()
    {
        var services = BuildServices(authenticationEnabled: true, useInternalAuthority: true);
        _services = services.Services;
        await using var provider = services.ServiceProvider;
    }

    [Fact] void should_mark_users_on_identity_cookie_and_bearer_issuance() => _services.Last(descriptor =>
        descriptor.ServiceType == typeof(IUserClaimsPrincipalFactory<StorageUser>)).ImplementationType.ShouldEqual(typeof(ChronicleUserClaimsPrincipalFactory));
}
