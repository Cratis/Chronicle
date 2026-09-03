// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;
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
[BelongsTo(WellKnownServices.Users)]
public record SetInitialAdminPassword(Guid UserId, string Password, string ConfirmedPassword)
{
    /// <summary>
    /// Handles the command by verifying the user has not yet logged in and appending a <see cref="UserPasswordChanged"/> event.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get event sequence grains with.</param>
    /// <param name="storage">The <see cref="IStorage"/> to load the user record from.</param>
    /// <param name="authentication">The authentication configuration that identifies the administrator.</param>
    /// <param name="eventSerializer">The event serializer.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="Services.Security.PasswordConfirmationMismatch">Thrown when the confirmed password does not match the password.</exception>
    /// <exception cref="Services.Security.UserNotFound">Thrown when the specified user does not exist.</exception>
    /// <exception cref="Services.Security.InitialPasswordCanOnlyBeSetForAdministrator">Thrown when the user is not the configured administrator.</exception>
    /// <exception cref="Services.Security.InitialPasswordAlreadySet">Thrown when the initial password has already been set.</exception>
    /// <exception cref="Services.Security.InitialPasswordCouldNotBeSet">Thrown when the password event could not be appended.</exception>
    internal async Task<EventSequenceNumber> Handle(
        IGrainFactory grainFactory,
        IStorage storage,
        Configuration.Authentication authentication,
        IEventSerializer eventSerializer)
    {
        if (Password != ConfirmedPassword)
        {
            throw new Services.Security.PasswordConfirmationMismatch();
        }

        var user = await storage.System.Users.GetById(UserId) ?? throw new Services.Security.UserNotFound(UserId);

        if (user.Username != (Username)authentication.EffectiveAdminUsername)
        {
            throw new Services.Security.InitialPasswordCanOnlyBeSetForAdministrator(UserId);
        }

        if (user.HasLoggedIn)
        {
            throw new Services.Security.InitialPasswordAlreadySet(UserId);
        }

        var passwordHash = new PasswordHasher<object>().HashPassword(null!, Password);
        var @event = new UserPasswordChanged((UserPassword)passwordHash);
        var eventSequence = grainFactory.GetEventLog();
        var concurrencyScope = new ConcurrencyScope(
            EventSequenceNumber.BeforeFirst,
            EventSourceId: true,
            EventStreamType: null,
            EventStreamId: null,
            EventSourceType: null,
            EventTypes: [typeof(UserPasswordChanged).GetEventType()]);
        var appendResult = await eventSequence.Append(
            EventSourceType.Default,
            UserId,
            EventStreamType.All,
            EventStreamId.Default,
            typeof(UserPasswordChanged).GetEventType(),
            eventSerializer.Serialize(@event),
            CorrelationId.New(),
            [],
            Identity.System,
            [],
            concurrencyScope);
        if (appendResult.HasConcurrencyViolations)
        {
            throw new Services.Security.InitialPasswordAlreadySet(UserId);
        }

        if (!appendResult.IsSuccess)
        {
            throw new Services.Security.InitialPasswordCouldNotBeSet(UserId);
        }

        return appendResult.SequenceNumber;
    }
}
