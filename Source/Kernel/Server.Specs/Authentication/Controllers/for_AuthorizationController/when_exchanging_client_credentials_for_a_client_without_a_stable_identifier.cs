// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController.given;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_exchanging_client_credentials_for_a_client_without_a_stable_identifier : a_client_credentials_token_endpoint
{
    IActionResult _result;

    void Establish()
    {
        var application = new object();
#pragma warning disable CA2012 // Configure substitute returns; these calls do not perform asynchronous work.
        _applicationManager.FindByClientIdAsync(ClientId, Arg.Any<CancellationToken>()).Returns(new ValueTask<object?>(application));
        _applicationManager.GetIdAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>(Guid.Empty.ToString()));
        _applicationManager.GetClientIdAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>(ClientId));
#pragma warning restore CA2012
    }

    async Task Because() => _result = await _controller.Exchange();

    [Fact] void should_forbid() => _result.ShouldBeOfExactType<ForbidResult>();
    [Fact] void should_reject_as_invalid_client() => ((ForbidResult)_result).Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldEqual(OpenIddictConstants.Errors.InvalidClient);
    [Fact] void should_not_record_an_authentication() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "Append").ShouldEqual(0);
}
