// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for removing a user.
/// </summary>
/// <param name="UserId">The unique identifier for the user.</param>
[Command]
public record RemoveUser(Guid UserId)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="users">The <see cref="IUsers"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IUsers users) =>
        users.Remove(new()
        {
            UserId = UserId
        });
}
