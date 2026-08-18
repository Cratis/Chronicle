// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents the command for removing a single external service.
/// </summary>
/// <param name="EventStore">The event store the external service belongs to.</param>
/// <param name="ExternalServiceId">The identifier of the external service to remove.</param>
[Command]
[BelongsTo(WellKnownServices.ExternalServices)]
public record RemoveExternalService(string EventStore, string ExternalServiceId)
{
    /// <summary>
    /// Handles the command by deleting the definition from the event store's external service storage.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IStorage storage) =>
        storage.GetEventStore(EventStore).ExternalServices.Delete(new ExternalServiceId(ExternalServiceId));
}
