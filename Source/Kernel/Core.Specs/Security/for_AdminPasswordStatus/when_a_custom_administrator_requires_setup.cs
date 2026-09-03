// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Options;

using StoredUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Security.for_AdminPasswordStatus;

public class when_a_custom_administrator_requires_setup : Specification
{
    static readonly Guid _administratorId = Guid.NewGuid();

    IStorage _storage;
    IUserStorage _users;
    AdminPasswordStatus _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _users = Substitute.For<IUserStorage>();
        _storage.System.Users.Returns(_users);
        _users.GetAll().Returns([
            new StoredUser
            {
                Id = _administratorId,
                Username = "chronicle-root",
                HasLoggedIn = false
            }
        ]);
    }

    async Task Because() =>
        _result = await AdminPasswordStatus.GetStatus(
            _storage,
            Options.Create(new Configuration.ChronicleOptions
            {
                Authentication = new()
                {
                    DefaultAdminUsername = "admin",
                    AdminUser = new() { Username = "chronicle-root" }
                }
            }));

    [Fact] void should_require_setup() => _result.IsRequired.ShouldBeTrue();
    [Fact] void should_return_the_configured_administrator() => _result.AdminUserId.ShouldEqual(_administratorId);
    [Fact] void should_return_the_configured_username() => _result.AdminUsername.ShouldEqual("chronicle-root");
}
