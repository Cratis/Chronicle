// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for adding a user.
/// </summary>
/// <param name="UserId">The user's unique identifier.</param>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password.</param>
[Command]
public record AddUser(
    Guid UserId,
    string Username,
    string Email,
    string Password)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="users">The <see cref="IUsers"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IUsers users) =>
        users.Add(new()
        {
            UserId = UserId,
            Username = Username,
            Email = Email,
            Password = Password
        });
}
