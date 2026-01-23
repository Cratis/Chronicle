// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the API for user commands.
/// </summary>
[Route("/api/users")]
public class UserCommands : ControllerBase
{
    readonly IChronicleConnection _connection;
    readonly PasswordHashingService _passwordHashingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCommands"/> class.
    /// </summary>
    /// <param name="connection">The <see cref="IChronicleConnection"/>.</param>
    /// <param name="passwordHashingService">The <see cref="PasswordHashingService"/>.</param>
    public UserCommands(IChronicleConnection connection, PasswordHashingService passwordHashingService)
    {
        _connection = connection;
        _passwordHashingService = passwordHashingService;
    }

    /// <summary>
    /// Change a user's password.
    /// </summary>
    /// <param name="command">The <see cref="ChangePassword"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("change-password")]
    public async Task<CommandResult> ChangePassword([FromBody] ChangePassword command)
    {
        var eventStore = await _connection.GetEventStore();
        var user = await eventStore.ReadModels.GetById<User>(command.UserId.ToString());

        if (user is null)
        {
            return CommandResult.Failed(["User not found"]);
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return CommandResult.Failed(["Password cannot be empty"]);
        }

        var passwordHash = _passwordHashingService.HashPassword(command.NewPassword);
        var @event = new PasswordChanged(command.UserId, passwordHash);
        
        await eventStore.EventLog.Append(
            command.UserId,
            @event);

        return CommandResult.Success();
    }

    /// <summary>
    /// Require a password change for a user.
    /// </summary>
    /// <param name="command">The <see cref="RequirePasswordChange"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("require-password-change")]
    public async Task<CommandResult> RequirePasswordChange([FromBody] RequirePasswordChange command)
    {
        var eventStore = await _connection.GetEventStore();
        var user = await eventStore.ReadModels.GetById<User>(command.UserId.ToString());

        if (user is null)
        {
            return CommandResult.Failed(["User not found"]);
        }

        var @event = new PasswordChangeRequired(command.UserId);
        
        await eventStore.EventLog.Append(
            command.UserId,
            @event);

        return CommandResult.Success();
    }

    /// <summary>
    /// Authenticate a user.
    /// </summary>
    /// <param name="command">The <see cref="Authenticate"/> command.</param>
    /// <returns>Authentication result with user information.</returns>
    [HttpPost("authenticate")]
    public async Task<ActionResult<AuthenticationResult>> Authenticate([FromBody] Authenticate command)
    {
        var eventStore = await _connection.GetEventStore();
        var users = await eventStore.ReadModels.GetAll<User>();
        var user = users.FirstOrDefault(u => u.Username == command.Username);

        if (user is null)
        {
            return Unauthorized(new AuthenticationResult(false, null, "Invalid username or password"));
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return Unauthorized(new AuthenticationResult(false, null, "Password not set. Please set a password first."));
        }

        if (!_passwordHashingService.VerifyPassword(command.Password, user.PasswordHash))
        {
            return Unauthorized(new AuthenticationResult(false, null, "Invalid username or password"));
        }

        if (!user.HasLoggedIn)
        {
            var @event = new UserLoggedIn(UserId.Parse(user.Id));
            await eventStore.EventLog.Append(UserId.Parse(user.Id), @event);
        }

        return Ok(new AuthenticationResult(true, UserId.Parse(user.Id), null)
        {
            PasswordChangeRequired = user.PasswordChangeRequired
        });
    }
}
