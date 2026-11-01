// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController.given;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_exchanging_client_credentials_for_an_unknown_client : a_client_credentials_token_endpoint
{
    IActionResult _result;

#pragma warning disable CA2012 // Configure substitute returns; this call does not perform asynchronous work.
    void Establish() =>
        _applicationManager.FindByClientIdAsync(ClientId, Arg.Any<CancellationToken>()).Returns(new ValueTask<object?>((object?)null));
#pragma warning restore CA2012

    async Task Because() => _result = await _controller.Exchange();

    [Fact] void should_forbid() => _result.ShouldBeOfExactType<ForbidResult>();
    [Fact] void should_reject_as_invalid_client() => ((ForbidResult)_result).Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldEqual(OpenIddictConstants.Errors.InvalidClient);
    [Fact] void should_forbid_with_the_openiddict_scheme() => ((ForbidResult)_result).AuthenticationSchemes.ShouldContainOnly(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    [Fact] void should_record_the_unknown_login_attempt() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "Append").ShouldEqual(1);
}
