// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController.given;
using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_exchanging_client_credentials_for_a_known_client : a_client_credentials_token_endpoint
{
    readonly Guid _applicationId = Guid.NewGuid();
    IActionResult _result;
    ClaimsPrincipal _principal;

    void Establish()
    {
        _request.ClientSecret = "client-secret";
        _request.Scope = "chronicle.read chronicle.write";
        var application = new object();
#pragma warning disable CA2012 // Configure substitute returns; these calls do not perform asynchronous work.
        _applicationManager.FindByClientIdAsync(ClientId, Arg.Any<CancellationToken>()).Returns(new ValueTask<object?>(application));
        _applicationManager.GetIdAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>(_applicationId.ToString()));
        _applicationManager.GetClientIdAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>(ClientId));
#pragma warning restore CA2012
    }

    async Task Because()
    {
        _result = await _controller.Exchange();
        _principal = ((SignInResult)_result).Principal;
    }

    [Fact] void should_sign_in() => _result.ShouldBeOfExactType<SignInResult>();
    [Fact] void should_sign_in_with_the_openiddict_scheme() => ((SignInResult)_result).AuthenticationScheme.ShouldEqual(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    [Fact] void should_resolve_to_the_configured_grant_holder() => Security.PrincipalKey.From(_principal, new Configuration.Authentication()).ShouldEqual(new Security.PrincipalKey(Security.PrincipalKey.LocalIssuer, _applicationId.ToString()));
    [Fact] void should_use_the_application_id_as_subject() => _principal.FindFirst(OpenIddictConstants.Claims.Subject).Value.ShouldEqual(_applicationId.ToString());
    [Fact] void should_use_the_client_id_as_name() => _principal.FindFirst(OpenIddictConstants.Claims.Name).Value.ShouldEqual(ClientId);
    [Fact] void should_carry_the_requested_scopes() => _principal.GetScopes().ShouldContainOnly("chronicle.read", "chronicle.write");
    [Fact] void should_target_the_chronicle_audience() => _principal.GetAudiences().ShouldContainOnly(WellKnownAudiences.Chronicle);
    [Fact] void should_send_name_to_access_and_identity_tokens() => _principal.FindFirst(OpenIddictConstants.Claims.Name).GetDestinations().ShouldContainOnly(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
}
