// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for removing a registered application.
/// </summary>
/// <param name="Id">The unique identifier of the application to remove.</param>
[Command]
public record RemoveApplication(Guid Id)
{
    internal async Task Handle(IGrainFactory grainFactory)
    {
        var @event = new ApplicationRemoved();
        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append((Concepts.Security.ApplicationId)Id, @event);
    }
}
