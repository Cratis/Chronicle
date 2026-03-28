// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for changing the password of an existing user.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="OldPassword">The current plain-text password for verification.</param>
/// <param name="Password">The new plain-text password.</param>
/// <param name="ConfirmedPassword">Confirmation of the new password; must match <paramref name="Password"/>.</param>
[Command]
public record ChangeUserPassword(Guid UserId, string OldPassword, string Password, string ConfirmedPassword)
{
    internal async Task Handle(IGrainFactory grainFactory, IStorage storage)
    {
        if (Password != ConfirmedPassword)
        {
            throw new Services.Security.PasswordConfirmationMismatch();
        }

        var user = await storage.System.Users.GetById(UserId) ?? throw new Services.Security.UserNotFound(UserId);

        var passwordHasher = new PasswordHasher<object>();
        if (user.PasswordHash is null || passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, OldPassword) != PasswordVerificationResult.Success)
        {
            throw new Services.Security.InvalidOldPassword();
        }

        if (passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, Password) == PasswordVerificationResult.Success)
        {
            throw new Services.Security.NewPasswordMustBeDifferent();
        }

        var passwordHash = passwordHasher.HashPassword(null!, Password);
        var @event = new UserPasswordChanged((UserPassword)passwordHash);
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append((Concepts.Security.UserId)UserId, @event);
    }
}
