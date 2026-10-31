// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using StorageUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController.given;

public class a_token_issuing_controller : Specification
{
    protected StorageUser _user;
    protected OpenIddictRequest _request;
    protected ClaimsPrincipal _issuedPrincipal;
    protected AuthorizationController _controller;
    ServiceProvider _provider;
    UserManager<StorageUser> _userManager;

    protected async Task ExchangeToken()
    {
        _controller.HttpContext.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = new OpenIddictServerTransaction { Request = _request } });
        _issuedPrincipal = ((Microsoft.AspNetCore.Mvc.SignInResult)await _controller.Exchange()).Principal;
    }

    void Establish()
    {
        _user = new StorageUser { Id = Guid.NewGuid(), Username = "operator", IsActive = true, HasLoggedIn = true };
        var options = Options.Create(new IdentityOptions());
        _userManager = Substitute.For<UserManager<StorageUser>>(
            Substitute.For<IUserStore<StorageUser>>(),
            options,
            new PasswordHasher<StorageUser>(),
            Array.Empty<IUserValidator<StorageUser>>(),
            Array.Empty<IPasswordValidator<StorageUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            NullLogger<UserManager<StorageUser>>.Instance);
        _userManager.FindByNameAsync("operator").Returns(_user);
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        var signInManager = Substitute.For<SignInManager<StorageUser>>(
            _userManager,
            new HttpContextAccessor(),
            Substitute.For<IUserClaimsPrincipalFactory<StorageUser>>(),
            options,
            NullLogger<SignInManager<StorageUser>>.Instance,
            Substitute.For<IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<StorageUser>>());
        signInManager.CheckPasswordSignInAsync(_user, Arg.Any<string>(), false).Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);
        signInManager.CanSignInAsync(_user).Returns(true);
        var applicationManager = Substitute.For<IOpenIddictApplicationManager>();
        var application = new object();
#pragma warning disable CA2012 // Configuring the substitute; the ValueTask is the stubbed call, not an operation to await
        applicationManager.FindByClientIdAsync("app-client").Returns(new ValueTask<object?>(application));
        applicationManager.GetIdAsync(application).Returns(new ValueTask<string?>(_user.Id.ToString()));
        applicationManager.GetClientIdAsync(application).Returns(new ValueTask<string?>("app-client"));
#pragma warning restore CA2012
        var authentication = Substitute.For<IAuthenticationService>();
        var priorUser = new ClaimsPrincipal(new ClaimsIdentity([new Claim(OpenIddictConstants.Claims.Subject, _user.Id.ToString())], "Bearer"));
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
            .Returns(AuthenticateResult.Success(new AuthenticationTicket(priorUser, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)));
        _provider = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IEventSequence>(Arg.Any<string>(), Arg.Any<string>()).Returns(Substitute.For<IEventSequence>());
        _controller = new AuthorizationController(_userManager, signInManager, applicationManager, grainFactory, NullLogger<AuthorizationController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = _provider } }
        };
    }

    async Task Destroy()
    {
        await _provider.DisposeAsync();
        _userManager.Dispose();
    }
}
