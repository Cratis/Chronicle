// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Options;
using StoredUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Security.for_AdminPasswordStatus;

public class when_a_trusted_proxy_signs_users_in : Specification
{
    IStorage _storage;
    IUserStorage _users;
    AdminPasswordStatus _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _users = Substitute.For<IUserStorage>();
        _storage.System.Users.Returns(_users);

        // An administrator that would otherwise require interactive setup.
        _users.GetByUsername("admin").Returns(
            new StoredUser
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                HasLoggedIn = false
            });
    }

    async Task Because() =>
        _result = await AdminPasswordStatus.GetStatus(
            _storage,
            Options.Create(new Configuration.ChronicleOptions
            {
                Authentication = new()
                {
                    Http = new() { Mode = HttpAuthenticationMode.TrustedProxy }
                }
            }));

    [Fact] void should_not_require_setup() => _result.IsRequired.ShouldBeFalse();
    [Fact] void should_not_return_an_administrator() => _result.AdminUserId.ShouldBeNull();
    [Fact] void should_not_return_a_username() => _result.AdminUsername.ShouldEqual(string.Empty);
    [Fact] void should_not_read_users() => _users.DidNotReceive().GetByUsername(Arg.Any<Username>());
}
