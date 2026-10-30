// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_issuing_a_password_grant : given.a_token_issuing_controller
{
    void Establish() => _request = new OpenIddictRequest { GrantType = OpenIddictConstants.GrantTypes.Password, Username = "operator", Password = "test-password" };

    Task Because() => ExchangeToken();

    [Fact] void should_mark_the_token_as_a_user() => _issuedPrincipal.FindFirst(ChronicleClaimTypes.Principal)!.Value.ShouldEqual(ChronicleClaimTypes.User);
    [Fact] void should_keep_the_user_subject() => _issuedPrincipal.FindFirst(OpenIddictConstants.Claims.Subject)!.Value.ShouldEqual(_user.Id.ToString());
}
