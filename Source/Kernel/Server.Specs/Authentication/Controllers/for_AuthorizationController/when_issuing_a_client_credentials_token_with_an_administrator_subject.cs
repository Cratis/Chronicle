// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_issuing_a_client_credentials_token_with_an_administrator_subject : given.a_token_issuing_controller
{
    void Establish() => _request = new OpenIddictRequest { GrantType = OpenIddictConstants.GrantTypes.ClientCredentials, ClientId = "app-client", ClientSecret = "test-client-secret" };

    Task Because() => ExchangeToken();

    [Fact] void should_keep_the_stable_application_subject() => _issuedPrincipal.FindFirst(OpenIddictConstants.Claims.Subject)!.Value.ShouldEqual(_user.Id.ToString());
    [Fact] void should_never_mark_the_application_as_a_user() => _issuedPrincipal.HasClaim(claim => claim.Type == ChronicleClaimTypes.Principal).ShouldBeFalse();

    [Fact]
    async Task should_deny_administrator_access_even_when_the_subject_matches_an_active_administrator()
    {
        var privileges = Substitute.For<IAdministratorPrivileges>();
        privileges.IsActiveAdministrator(_user.Id).Returns(true);
        var authorization = new AdministratorAuthorization(Options.Create(new ChronicleOptions()), privileges, NullLogger<AdministratorAuthorization>.Instance);
        (await authorization.IsAuthorized(_issuedPrincipal)).ShouldBeFalse();
        await privileges.DidNotReceive().IsActiveAdministrator(_user.Id);
    }
}
