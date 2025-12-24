// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Grains.Security;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles user events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UsersReactor"/> class.
/// </remarks>
/// <param name="userStorage">The <see cref="IUserStorage"/> for managing users.</param>
public class UsersReactor(IUserStorage userStorage) : Reactor
{
    /// <summary>
    /// Handles the addition of a user.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(UserAdded @event, EventContext eventContext)
    {
        var user = new ChronicleUser
        {
            Id = @event.UserId,
            Username = @event.Username,
            Email = @event.Email,
            PasswordHash = @event.PasswordHash,
            SecurityStamp = Guid.NewGuid().ToString(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = null
        };

        await userStorage.Create(user);
    }

    /// <summary>
    /// Handles the removal of a user.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Removed(UserRemoved @event, EventContext eventContext)
    {
        await userStorage.Delete(@event.UserId);
    }

    /// <summary>
    /// Handles the password change of a user.
    /// </summary>
    /// <param name="event">The event containing the user information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task PasswordChanged(UserPasswordChanged @event, EventContext eventContext)
    {
        var user = await userStorage.GetById(@event.UserId);
        if (user is not null)
        {
            user.PasswordHash = @event.PasswordHash;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.LastModifiedAt = DateTimeOffset.UtcNow;
            await userStorage.Update(user);
        }
    }
}
