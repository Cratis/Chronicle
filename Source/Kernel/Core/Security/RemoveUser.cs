// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for removing a user from the system.
/// </summary>
/// <param name="UserId">The unique identifier of the user to remove.</param>
[Command]
public record RemoveUser(Guid UserId)
{
    internal async Task Handle(IGrainFactory grainFactory)
    {
        var @event = new UserRemoved();
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append((Concepts.Security.UserId)UserId, @event);
    }
}
