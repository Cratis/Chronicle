// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleSignInManager.given;

public class a_password_sign_in : Specification
{
    protected SignInManager<User> _manager;
    protected IPasswordHasher<User> _hasher;
    protected ISignInThrottle _throttle;
    protected SignInResult _result;
    ServiceProvider _services;

    void Establish()
    {
        _throttle = Substitute.For<ISignInThrottle>();
        _hasher = Substitute.For<IPasswordHasher<User>>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.AddSingleton(Substitute.For<IUserStorage>());
        services.AddSingleton(_throttle);
        services.AddIdentityCore<User>().AddUserStore<UserStore>().AddSignInManager<ChronicleSignInManager>();
        services.AddSingleton(_hasher);
        _services = services.BuildServiceProvider();
        _manager = _services.GetRequiredService<SignInManager<User>>();
        _manager.Context = new DefaultHttpContext();
    }

    protected async Task SignIn() => _result = await _manager.CheckPasswordSignInAsync(
        new User { Username = "admin", PasswordHash = "hash" }, "any-password", false);

    void Destroy() => _services.Dispose();
}
