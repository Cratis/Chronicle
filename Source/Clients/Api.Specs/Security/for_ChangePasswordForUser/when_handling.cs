// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security.for_ChangePasswordForUser;

/// <summary>
/// The kernel verifies the current password before it changes it, so every value the user typed - the
/// old password included - has to reach the request. Dropping one leaves the change rejected as invalid.
/// </summary>
public class when_handling : Specification
{
    static readonly Guid _userId = Guid.Parse("3c1d2e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f");
    const string OldPassword = "the-old-password";
    const string Password = "the-new-password";
    const string ConfirmedPassword = "the-new-password";

    IUsers _users;
    ChangePasswordForUser _command;

    void Establish()
    {
        _users = Substitute.For<IUsers>();
        _users.ChangeUserPassword(Arg.Any<ChangeUserPasswordRequest>()).Returns(CommandResult.Success(Guid.NewGuid()));
        _command = new ChangePasswordForUser(_userId, OldPassword, Password, ConfirmedPassword);
    }

    async Task Because() => await _command.Handle(_users);

    [Fact] void should_forward_every_value_to_the_kernel() => _users.Received(1).ChangeUserPassword(
        Arg.Is<ChangeUserPasswordRequest>(request =>
            request.UserId == _userId &&
            request.OldPassword == OldPassword &&
            request.Password == Password &&
            request.ConfirmedPassword == ConfirmedPassword));
}
