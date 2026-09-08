// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for rotating the client secret of a registered application.
/// </summary>
/// <param name="Id">The unique identifier of the application.</param>
/// <param name="ClientSecret">The new plain-text client secret to be hashed and stored.</param>
[Command]
[BelongsTo(WellKnownServices.Applications)]
public record ChangeApplicationSecret(Concepts.Security.ApplicationId Id, ClientSecret ClientSecret)
{
    /// <summary>
    /// Handles the command by appending an <see cref="ApplicationSecretChanged"/> event to the event log.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get event sequence grains with.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(IGrainFactory grainFactory)
    {
        var hashedSecret = new PasswordHasher<object>().HashPassword(null!, ClientSecret);
        var @event = new ApplicationSecretChanged((ClientSecret)hashedSecret);
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(Id, @event);
    }
}
