// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.Identity;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;
using ClientId = Cratis.Chronicle.Concepts.Security.ClientId;
using ClientSecret = Cratis.Chronicle.Concepts.Security.ClientSecret;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for registering a new application (OAuth client).
/// </summary>
/// <param name="Id">The unique identifier for the application.</param>
/// <param name="ClientId">The OAuth client identifier.</param>
/// <param name="ClientSecret">The plain-text client secret to be hashed and stored.</param>
[Command]
[BelongsTo(WellKnownServices.Applications)]
public record AddApplication(Guid Id, string ClientId, string ClientSecret)
{
    /// <summary>
    /// Handles the command by appending an <see cref="ApplicationAdded"/> event to the event log.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get event sequence grains with.</param>
    /// <param name="storage">The <see cref="IStorage"/> to check existing applications in.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="Services.Security.ApplicationClientIdAlreadyRegistered">Thrown when an application with the same client identifier is already registered.</exception>
    public async Task Handle(IGrainFactory grainFactory, IStorage storage)
    {
        var existing = await storage.System.Applications.GetByClientId((ClientId)ClientId);
        if (existing is not null)
        {
            throw new Services.Security.ApplicationClientIdAlreadyRegistered(ClientId);
        }

        var hashedSecret = new PasswordHasher<object>().HashPassword(null!, ClientSecret);
        var @event = new ApplicationAdded((ClientId)ClientId, (ClientSecret)hashedSecret);
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append((ApplicationId)Id, @event);
    }
}
