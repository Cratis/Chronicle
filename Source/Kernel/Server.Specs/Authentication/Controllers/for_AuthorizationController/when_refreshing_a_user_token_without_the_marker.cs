// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_refreshing_a_user_token_without_the_marker : given.a_token_issuing_controller
{
    void Establish() => _request = new OpenIddictRequest { GrantType = OpenIddictConstants.GrantTypes.RefreshToken, RefreshToken = "test-refresh-token" };

    Task Because() => ExchangeToken();

    [Fact] void should_add_the_user_marker_after_validating_the_user() => _issuedPrincipal.FindFirst(ChronicleClaimTypes.Principal)!.Value.ShouldEqual(ChronicleClaimTypes.User);
    [Fact] void should_keep_the_user_subject() => _issuedPrincipal.FindFirst(OpenIddictConstants.Claims.Subject)!.Value.ShouldEqual(_user.Id.ToString());
}
