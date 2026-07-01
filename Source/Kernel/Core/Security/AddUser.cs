// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for adding a new user to the system.
/// </summary>
/// <param name="UserId">The unique identifier for the user.</param>
/// <param name="Username">The username.</param>
/// <param name="Email">The email address.</param>
/// <param name="Password">The plain-text password to be hashed and stored.</param>
[Command]
[BelongsTo(WellKnownServices.Users)]
public record AddUser(Guid UserId, string Username, string Email, string Password)
{
    /// <summary>
    /// Handles the command by appending a <see cref="UserAdded"/> event to the event log.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get event sequence grains with.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IGrainFactory grainFactory)
    {
        var passwordHasher = new PasswordHasher<object>();
        var passwordHash = passwordHasher.HashPassword(null!, Password);
        var @event = new UserAdded((Username)Username, (UserEmail)Email, (UserPassword)passwordHash);
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(UserId, @event);
    }
}
