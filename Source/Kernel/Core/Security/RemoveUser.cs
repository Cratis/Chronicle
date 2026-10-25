// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the command for removing a user from the system.
/// </summary>
/// <param name="UserId">The unique identifier of the user to remove.</param>
[Command]
[BelongsTo(WellKnownServices.Users)]
public record RemoveUser(UserId UserId)
{
    /// <summary>
    /// Handles the command by removing the user through the <see cref="IAdministratorRoster"/>, which refuses to remove the last active administrator.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the administrator roster with.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="LastAdministratorCannotBeRemoved">Thrown when the user is the last active administrator.</exception>
    /// <remarks>
    /// The handler returns no value so the command keeps its existing wire contract; the refusal surfaces as a failed command result.
    /// </remarks>
    public async Task Handle(IGrainFactory grainFactory)
    {
        var removed = await grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).RemoveUser(UserId);
        if (!removed)
        {
            throw new LastAdministratorCannotBeRemoved();
        }
    }
}
