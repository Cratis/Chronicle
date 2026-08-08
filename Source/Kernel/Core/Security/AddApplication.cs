// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;
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
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IGrainFactory grainFactory)
    {
        var hashedSecret = new PasswordHasher<object>().HashPassword(null!, ClientSecret);
        var @event = new ApplicationAdded((ClientId)ClientId, (ClientSecret)hashedSecret);
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append((ApplicationId)Id, @event);
    }
}
