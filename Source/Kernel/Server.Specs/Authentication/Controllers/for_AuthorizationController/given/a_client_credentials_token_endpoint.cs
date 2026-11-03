// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController.given;

public class a_client_credentials_token_endpoint : Specification
{
    protected const string ClientId = "my-client";

    protected IOpenIddictApplicationManager _applicationManager;
    protected IEventSequence _eventLog;
    protected OpenIddictRequest _request;
    protected AuthorizationController _controller;

    void Establish()
    {
        _applicationManager = Substitute.For<IOpenIddictApplicationManager>();
        _eventLog = Substitute.For<IEventSequence>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventLog);

        _request = new OpenIddictRequest
        {
            GrantType = OpenIddictConstants.GrantTypes.ClientCredentials,
            ClientId = ClientId
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = _request }
        });

        _controller = new AuthorizationController(
            Substitute.For<UserManager<User>>(Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null),
            Substitute.For<SignInManager<User>>(
                Substitute.For<UserManager<User>>(Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null),
                Substitute.For<IHttpContextAccessor>(),
                Substitute.For<IUserClaimsPrincipalFactory<User>>(),
                null,
                null,
                null,
                null),
            _applicationManager,
            grainFactory,
            Substitute.For<ILogger<AuthorizationController>>())
        {
            ControllerContext = new() { HttpContext = httpContext }
        };
    }
}
