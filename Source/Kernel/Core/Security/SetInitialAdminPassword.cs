// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for setting the initial admin password for a user who has not yet logged in.
/// </summary>
/// <param name="UserId">The unique identifier of the admin user.</param>
/// <param name="Password">The plain-text password to set.</param>
/// <param name="ConfirmedPassword">Confirmation of the password; must match <paramref name="Password"/>.</param>
[Command]
[AllowAnonymous]
[BelongsTo(WellKnownServices.Users)]
public record SetInitialAdminPassword(UserId UserId, Password Password, Password ConfirmedPassword)
{
    /// <summary>
    /// Handles the command by verifying the user has not yet logged in and appending a <see cref="UserPasswordChanged"/> event.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get event sequence grains with.</param>
    /// <param name="storage">The <see cref="IStorage"/> to load the user record from.</param>
    /// <param name="options">The configured administrator.</param>
    /// <param name="eventSerializer">The event serializer.</param>
    /// <returns>Awaitable task, completing only when the credentials are persisted.</returns>
    /// <exception cref="PasswordConfirmationMismatch">Thrown when the confirmed password does not match the password.</exception>
    /// <exception cref="UserNotFound">Thrown when the specified user does not exist.</exception>
    /// <exception cref="InitialAdminPasswordAlreadyUsed">Thrown when the user has already logged in and has an initial password set.</exception>
    /// <exception cref="InitialPasswordCanOnlyBeSetForAdministrator">Thrown for a different administrator identity.</exception>
    /// <exception cref="InitialPasswordCouldNotBeSet">Thrown when appending fails.</exception>
    public async Task Handle(IGrainFactory grainFactory, IStorage storage, IOptions<Configuration.ChronicleOptions> options, IEventSerializer eventSerializer)
    {
        if (Password != ConfirmedPassword)
        {
            throw new PasswordConfirmationMismatch();
        }

        var user = await storage.System.Users.GetById(UserId) ?? throw new UserNotFound(UserId);

        var administrator = await storage.System.Users.GetByUsername(options.Value.Authentication.EffectiveAdminUsername);
        if (administrator is null || administrator.Id != UserId)
        {
            throw new InitialPasswordCanOnlyBeSetForAdministrator(UserId);
        }

        // Older deployments also created temporary-password users with HasLoggedIn=false.
        // Never let initial setup replace any existing credential, even in that legacy state.
        if (user.HasLoggedIn || !string.IsNullOrEmpty(user.PasswordHash?.Value))
        {
            throw new InitialAdminPasswordAlreadyUsed();
        }

        var passwordHash = new PasswordHasher<object>().HashPassword(null!, Password);
        var @event = new UserPasswordChanged((UserPassword)passwordHash);
        var eventSequence = grainFactory.GetEventLog();
        var eventType = typeof(UserPasswordChanged).GetEventType();
        var result = await eventSequence.Append(
            EventSourceType.Default,
            UserId,
            EventStreamType.All,
            EventStreamId.Default,
            eventType,
            eventSerializer.Serialize(@event),
            CorrelationId.New(),
            [],
            Identity.System,
            [],
            new ConcurrencyScope(EventSequenceNumber.BeforeFirst, EventSourceId: true, EventStreamType: null, EventStreamId: null, EventSourceType: null, EventTypes: [eventType]));
        if (result.HasConcurrencyViolations)
        {
            throw new InitialAdminPasswordAlreadyUsed();
        }
        if (!result.IsSuccess)
        {
            throw new InitialPasswordCouldNotBeSet(UserId);
        }

        await UserProjection.WaitForPassword(storage.System.Users, UserId, passwordHash);
    }
}
