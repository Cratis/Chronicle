// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the API for commands related to users.
/// </summary>
[Route("/api/security/users")]
public class UserCommands : ControllerBase
{
    readonly IUsers _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCommands"/> class.
    /// </summary>
    /// <param name="users">The <see cref="IUsers"/> contract.</param>
    internal UserCommands(IUsers users)
    {
        _users = users;
    }

    /// <summary>
    /// Add a user.
    /// </summary>
    /// <param name="command"><see cref="AddUser"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("add")]
    public Task Add([FromBody] AddUser command) =>
        _users.Add(new()
        {
            UserId = command.UserId,
            Username = command.Username,
            Email = command.Email,
            Password = command.Password
        });

    /// <summary>
    /// Remove a user.
    /// </summary>
    /// <param name="command"><see cref="RemoveUser"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("remove")]
    public Task Remove([FromBody] RemoveUser command) =>
        _users.Remove(new() { UserId = command.UserId });

    /// <summary>
    /// Change a user's password.
    /// </summary>
    /// <param name="command"><see cref="ChangePasswordForUser"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("change-password")]
    public Task ChangePassword([FromBody] ChangePasswordForUser command) =>
        _users.ChangePassword(new()
        {
            UserId = command.UserId,
            Password = command.Password
        });
}
