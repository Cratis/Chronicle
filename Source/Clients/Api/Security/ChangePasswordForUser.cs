// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for changing a user's password.
/// </summary>
/// <param name="UserId">The user's unique identifier.</param>
/// <param name="oldPassword">The old password.</param>
/// <param name="Password">The new password.</param>
/// <param name="ConfirmedPassword">The confirmed new password.</param>
[Command]
[AllowAnonymous]
public record ChangePasswordForUser(
    Guid UserId,
    string oldPassword,
    string Password,
    string ConfirmedPassword)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="users">The <see cref="IUsers"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IUsers users) =>
        users.ChangePassword(new()
        {
            UserId = UserId,
            OldPassword = oldPassword,
            Password = Password,
            ConfirmedPassword = ConfirmedPassword
        });
}

