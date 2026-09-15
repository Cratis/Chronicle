// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_LoginController;

public class when_a_legacy_user_has_a_temporary_password : Specification
{
    ServiceProvider _services;
    IUserStorage _users;
    LoginResponse _result;

    void Establish()
    {
        _users = Substitute.For<IUserStorage>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "legacy-user",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, "temporary-password"),
            HasLoggedIn = false,
            RequiresPasswordChange = true,
            IsActive = true
        };
        _users.GetByUsername(Arg.Any<Username>()).Returns(user);
        _users.GetById(user.Id).Returns(user);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.AddSingleton(_users);
        services.AddIdentityCore<User>().AddUserStore<UserStore>().AddSignInManager();
        _services = services.BuildServiceProvider();
    }

    async Task Because() => _result = await new LoginController(
        _services.GetRequiredService<UserManager<User>>(),
        _services.GetRequiredService<SignInManager<User>>(),
        _users).Login(new LoginRequest { Username = "legacy-user", Password = "temporary-password" });

    [Fact] void should_allow_existing_credentials() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_password_change_prompt() => _result.RequiresPasswordChange.ShouldBeTrue();

    void Destroy() => _services.Dispose();
}
