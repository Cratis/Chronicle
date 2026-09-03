// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security.for_InitialAdminPasswordSetupStatus;

public class when_getting_status : Specification
{
    static readonly Guid _administratorId = Guid.NewGuid();

    IUsers _users;
    InitialAdminPasswordSetupStatus _result;

    void Establish()
    {
        _users = Substitute.For<IUsers>();
        _users.GetStatus().Returns(QueryResult<AdminPasswordStatusResponse>.Success(
            Guid.NewGuid(),
            new AdminPasswordStatusResponse
            {
                IsRequired = true,
                AdminUserId = _administratorId,
                AdminUsername = "chronicle-root"
            }));
    }

    async Task Because() => _result = await InitialAdminPasswordSetupStatus.GetStatus(_users);

    [Fact] void should_return_the_administrator_id() => _result.AdminUserId.ShouldEqual(_administratorId);
    [Fact] void should_return_the_administrator_username() => _result.AdminUsername.ShouldEqual("chronicle-root");
}
