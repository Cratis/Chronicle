// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents the command for removing external services from an event store.
/// </summary>
/// <param name="EventStore">The event store the external services belong to.</param>
/// <param name="ExternalServices">The identifiers of the external services to remove.</param>
[Command]
[BelongsTo(WellKnownServices.ExternalServices)]
public record RemoveExternalServices(EventStoreName EventStore, IEnumerable<ExternalServiceId> ExternalServices)
{
    /// <summary>
    /// Handles the command by deleting each identified definition from the event store's external service storage.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IStorage storage)
    {
        var externalServices = storage.GetEventStore(EventStore).ExternalServices;
        foreach (var id in ExternalServices)
        {
            await externalServices.Delete(id);
        }
    }
}
