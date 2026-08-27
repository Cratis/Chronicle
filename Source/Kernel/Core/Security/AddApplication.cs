// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for registering a new application (OAuth client).
/// </summary>
/// <param name="Id">The unique identifier for the application.</param>
/// <param name="ClientId">The OAuth client identifier.</param>
/// <param name="ClientSecret">The plain-text client secret to be hashed and stored.</param>
[Command]
[BelongsTo(WellKnownServices.Applications)]
public record AddApplication(Concepts.Security.ApplicationId Id, ClientId ClientId, ClientSecret ClientSecret)
{
    /// <summary>
    /// Handles the command by appending an <see cref="ApplicationAdded"/> event to the event log.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get event sequence grains with.</param>
    /// <param name="storage">The <see cref="IStorage"/> to check existing applications in.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ApplicationClientIdAlreadyRegistered">Thrown when an application with the same client identifier is already registered.</exception>
    public async Task Handle(IGrainFactory grainFactory, IStorage storage)
    {
        var existing = await storage.System.Applications.GetByClientId(ClientId);
        if (existing is not null)
        {
            throw new ApplicationClientIdAlreadyRegistered(ClientId);
        }

        var hashedSecret = new PasswordHasher<object>().HashPassword(null!, ClientSecret);
        var @event = new ApplicationAdded(ClientId, (ClientSecret)hashedSecret);
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(Id, @event);
    }
}
