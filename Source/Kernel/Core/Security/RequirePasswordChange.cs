// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for requiring a user to change their password on next login.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
[Command]
public record RequirePasswordChange(Guid UserId)
{
    /// <summary>
    /// Handles the command by appending a <see cref="PasswordChangeRequired"/> event to the event log.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get event sequence grains with.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IGrainFactory grainFactory)
    {
        var @event = new PasswordChangeRequired();
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(UserId, @event);
    }
}
