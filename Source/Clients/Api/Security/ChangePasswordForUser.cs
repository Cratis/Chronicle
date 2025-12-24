// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for changing a user's password.
/// </summary>
/// <param name="UserId">The user's unique identifier.</param>
/// <param name="Password">The new password.</param>
[Command]
public record ChangePasswordForUser(
    string UserId,
    string Password)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="users">The <see cref="IUsers"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IUsers users) =>
        users.ChangePassword(new()
        {
            UserId = UserId,
            Password = Password
        });
}

