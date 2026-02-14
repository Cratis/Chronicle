// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles user events.
/// </summary>
/// <param name="userStorage">The <see cref="IUserStorage"/> for managing users.</param>
/// <param name="logger">The <see cref="ILogger{UsersReactor}"/> for logging.</param>
public class UsersReactor(IUserStorage userStorage, ILogger<UsersReactor> logger) : Reactor
{
    /// <summary>
    /// Handles the addition of a user.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(UserAdded @event, EventContext eventContext)
    {
        logger.AddingUser(eventContext.EventSourceId, @event.Username);

        var user = new User
        {
            Id = eventContext.EventSourceId,
            Username = @event.Username,
            Email = @event.Email,
            PasswordHash = @event.PasswordHash,
            SecurityStamp = Guid.NewGuid().ToString(),
            IsActive = true,
            RequiresPasswordChange = true,
            HasLoggedIn = false,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = null
        };

        await userStorage.Create(user);

        logger.UserAdded(eventContext.EventSourceId);
    }

    /// <summary>
    /// Handles the addition of the initial admin user without a password.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task InitialAdminAdded(InitialAdminUserAdded @event, EventContext eventContext)
    {
        logger.AddingInitialAdminUser(eventContext.EventSourceId, @event.Username ?? "[not set]");

        var user = new User
        {
            Id = eventContext.EventSourceId,
            Username = @event.Username ?? string.Empty,
            Email = @event.Email ?? string.Empty,
            PasswordHash = null,
            SecurityStamp = Guid.NewGuid().ToString(),
            IsActive = true,
            RequiresPasswordChange = true,
            HasLoggedIn = false,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = null
        };

        await userStorage.Create(user);

        logger.InitialAdminUserAdded(eventContext.EventSourceId);
    }

    /// <summary>
    /// Handles the removal of a user.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Removed(UserRemoved @event, EventContext eventContext)
    {
        logger.RemovingUser(eventContext.EventSourceId);

        await userStorage.Delete(eventContext.EventSourceId);

        logger.UserRemoved(eventContext.EventSourceId);
    }

    /// <summary>
    /// Handles the password change of a user.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task PasswordChanged(UserPasswordChanged @event, EventContext eventContext)
    {
        logger.ChangingPassword(eventContext.EventSourceId);

        var user = await userStorage.GetById(eventContext.EventSourceId);
        if (user is not null)
        {
            user.PasswordHash = @event.PasswordHash;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.RequiresPasswordChange = false;
            user.HasLoggedIn = true;
            user.LastModifiedAt = DateTimeOffset.UtcNow;
            await userStorage.Update(user);

            logger.PasswordChanged(eventContext.EventSourceId);
        }
        else
        {
            logger.UserNotFoundWhenChangingPassword(eventContext.EventSourceId);
        }
    }

    /// <summary>
    /// Handles when a user is required to change their password.
    /// </summary>
    /// <param name="event">The event containing the requirement information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task RequiresPasswordChange(PasswordChangeRequired @event, EventContext eventContext)
    {
        logger.SettingPasswordChangeRequirement(eventContext.EventSourceId);

        var user = await userStorage.GetById(eventContext.EventSourceId);
        if (user is not null)
        {
            user.RequiresPasswordChange = true;
            user.LastModifiedAt = DateTimeOffset.UtcNow;
            await userStorage.Update(user);

            logger.PasswordChangeRequirementSet(eventContext.EventSourceId);
        }
        else
        {
            logger.UserNotFoundWhenSettingPasswordChangeRequirement(eventContext.EventSourceId);
        }
    }
}
