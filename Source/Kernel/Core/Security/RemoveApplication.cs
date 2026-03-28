// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.EventSequences;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for removing a registered application.
/// </summary>
/// <param name="Id">The unique identifier of the application to remove.</param>
[Command]
public record RemoveApplication(Guid Id)
{
    /// <summary>
    /// Handles the command by appending an <see cref="ApplicationRemoved"/> event to the event log.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get event sequence grains with.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IGrainFactory grainFactory)
    {
        var @event = new ApplicationRemoved();
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append((ApplicationId)Id, @event);
    }
}
