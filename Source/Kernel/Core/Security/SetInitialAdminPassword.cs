// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for setting the initial admin password for a user who has not yet logged in.
/// </summary>
/// <param name="UserId">The unique identifier of the admin user.</param>
/// <param name="Password">The plain-text password to set.</param>
/// <param name="ConfirmedPassword">Confirmation of the password; must match <paramref name="Password"/>.</param>
[Command]
public record SetInitialAdminPassword(Guid UserId, string Password, string ConfirmedPassword)
{
    internal async Task Handle(IGrainFactory grainFactory, IStorage storage)
    {
        if (Password != ConfirmedPassword)
        {
            throw new Services.Security.PasswordConfirmationMismatch();
        }

        var user = await storage.System.Users.GetById(UserId) ?? throw new Services.Security.UserNotFound(UserId);

        if (user.HasLoggedIn)
        {
            throw new InvalidOperationException("Setting the initial admin password is only allowed for users who have not yet logged in.");
        }

        var passwordHash = new PasswordHasher<object>().HashPassword(null!, Password);
        var @event = new UserPasswordChanged((UserPassword)passwordHash);
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append((Concepts.Security.UserId)UserId, @event);
    }
}
