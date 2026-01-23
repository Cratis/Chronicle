// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the API for user commands.
/// </summary>
/// <param name="client">The <see cref="IChronicleClient"/>.</param>
/// <param name="passwordHashingService">The <see cref="PasswordHashingService"/>.</param>
[Route("/api/users")]
public class UserCommands(IChronicleClient client, PasswordHashingService passwordHashingService) : ControllerBase
{
    /// <summary>
    /// Change a user's password.
    /// </summary>
    /// <param name="command">The <see cref="ChangePassword"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("change-password")]
    public async Task ChangePassword([FromBody] ChangePassword command)
    {
        var eventStore = await client.GetEventStore("System");

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            throw new ArgumentException("Password cannot be empty");
        }

        var passwordHash = passwordHashingService.HashPassword(command.NewPassword);
        var @event = new PasswordChanged(command.UserId, passwordHash);

        await eventStore.EventLog.Append(
            command.UserId,
            @event);
    }

    /// <summary>
    /// Require a password change for a user.
    /// </summary>
    /// <param name="command">The <see cref="RequirePasswordChange"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("require-password-change")]
    public async Task RequirePasswordChange([FromBody] RequirePasswordChange command)
    {
        var eventStore = await client.GetEventStore("System");
        var @event = new PasswordChangeRequired(command.UserId);

        await eventStore.EventLog.Append(
            command.UserId,
            @event);
    }

    /// <summary>
    /// Initialize the admin user.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [HttpPost("initialize-admin")]
    public async Task InitializeAdmin()
    {
        var eventStore = await client.GetEventStore("System");
        var @event = new InitialAdminUserAdded(UserId.Admin, "admin");

        await eventStore.EventLog.Append(
            UserId.Admin,
            @event);
    }
}
