// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using StorageUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController.given;

public class a_token_request : Specification
{
    protected ServiceProvider _services;
    protected AuthorizationController _controller;
    protected IOpenIddictApplicationManager _applications;
    protected IAuthenticationService _authentication;
    protected StorageUser _user;
    protected OpenIddictRequest _request;
    protected Microsoft.AspNetCore.Mvc.SignInResult _result;

    void Establish()
    {
        var users = Substitute.For<IUserStorage>();
        _user = new StorageUser { Id = Guid.NewGuid(), Username = "operator", IsActive = true, PasswordHash = new PasswordHasher<StorageUser>().HashPassword(null!, "long-password") };
        users.GetByUsername(Arg.Any<Concepts.Security.Username>()).Returns(_user);
        users.GetById(_user.Id).Returns(_user);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        _authentication = Substitute.For<IAuthenticationService>();
        services.AddSingleton(_authentication);
        services.AddSingleton(users);
        services.AddIdentityCore<StorageUser>().AddUserStore<UserStore>().AddSignInManager();
        _services = services.BuildServiceProvider();
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(Substitute.For<IEventSequence>());
        _applications = Substitute.For<IOpenIddictApplicationManager>();
        _request = new OpenIddictRequest { GrantType = OpenIddictConstants.GrantTypes.Password, Username = "operator", Password = "long-password" };
        var context = new DefaultHttpContext { RequestServices = _services };
        context.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = new OpenIddictServerTransaction { Request = _request } });
        _controller = new(_services.GetRequiredService<UserManager<StorageUser>>(), _services.GetRequiredService<SignInManager<StorageUser>>(), _applications, grains, NullLogger<AuthorizationController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    void Destroy() => _services.Dispose();
}
