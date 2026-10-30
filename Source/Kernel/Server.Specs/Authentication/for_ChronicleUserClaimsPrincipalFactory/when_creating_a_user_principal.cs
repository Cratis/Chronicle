// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StorageUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleUserClaimsPrincipalFactory;

public class when_creating_a_user_principal : Specification
{
    UserManager<StorageUser> _manager;
    ChronicleUserClaimsPrincipalFactory _factory;
    StorageUser _user;
    ClaimsPrincipal _principal;

    void Establish()
    {
        _user = new StorageUser { Id = Guid.NewGuid(), Username = "operator" };
        var store = Substitute.For<IUserStore<StorageUser>>();
        store.GetUserIdAsync(_user, Arg.Any<CancellationToken>()).Returns(_user.Id.ToString());
        store.GetUserNameAsync(_user, Arg.Any<CancellationToken>()).Returns("operator");
        var options = Options.Create(new IdentityOptions());
        _manager = new UserManager<StorageUser>(
            store,
            options,
            new PasswordHasher<StorageUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            NullLogger<UserManager<StorageUser>>.Instance);
        _factory = new ChronicleUserClaimsPrincipalFactory(_manager, options);
    }

    async Task Because() => _principal = await _factory.CreateAsync(_user);

    [Fact] void should_issue_an_authenticated_identity() => _principal.Identity!.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_mark_the_principal_as_a_user() => _principal.FindFirst(ChronicleClaimTypes.Principal)!.Value.ShouldEqual(ChronicleClaimTypes.User);
    [Fact] void should_keep_the_identity_user_identifier_for_administrator_lookup() => _principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.ShouldEqual(_user.Id.ToString());

    void Destroy() => _manager.Dispose();
}
