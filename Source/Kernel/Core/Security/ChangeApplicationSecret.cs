// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Identity;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;
using ClientSecret = Cratis.Chronicle.Concepts.Security.ClientSecret;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for rotating the client secret of a registered application.
/// </summary>
/// <param name="Id">The unique identifier of the application.</param>
/// <param name="ClientSecret">The new plain-text client secret to be hashed and stored.</param>
[Command]
public record ChangeApplicationSecret(Guid Id, string ClientSecret)
{
    /// <summary>
    /// Handles the command by appending an <see cref="ApplicationSecretChanged"/> event to the event log.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get event sequence grains with.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IGrainFactory grainFactory)
    {
        var hashedSecret = new PasswordHasher<object>().HashPassword(null!, ClientSecret);
        var @event = new ApplicationSecretChanged((ClientSecret)hashedSecret);
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append((ApplicationId)Id, @event);
    }
}
