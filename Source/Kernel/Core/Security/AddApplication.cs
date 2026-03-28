// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for registering a new application (OAuth client).
/// </summary>
/// <param name="Id">The unique identifier for the application.</param>
/// <param name="ClientId">The OAuth client identifier.</param>
/// <param name="ClientSecret">The plain-text client secret to be hashed and stored.</param>
[Command]
public record AddApplication(Guid Id, string ClientId, string ClientSecret)
{
    internal async Task Handle(IGrainFactory grainFactory)
    {
        var hashedSecret = new PasswordHasher<object>().HashPassword(null!, ClientSecret);
        var @event = new ApplicationAdded((Concepts.Security.ClientId)ClientId, (Concepts.Security.ClientSecret)hashedSecret);
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append((Concepts.Security.ApplicationId)Id, @event);
    }
}
