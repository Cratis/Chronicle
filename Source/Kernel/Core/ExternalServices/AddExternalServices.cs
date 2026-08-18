// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents the command for registering external services with an event store.
/// </summary>
/// <param name="EventStore">The event store the external services belong to.</param>
/// <param name="ExternalServices">The external services to register.</param>
[Command]
[BelongsTo(WellKnownServices.ExternalServices)]
public record AddExternalServices(
    string EventStore,
    IEnumerable<Contracts.ExternalServices.ExternalServiceDefinition> ExternalServices)
{
    /// <summary>
    /// Handles the command by saving each definition into the event store's external service storage.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IStorage storage)
    {
        var externalServices = storage.GetEventStore(EventStore).ExternalServices;
        foreach (var externalService in ExternalServices)
        {
            await externalServices.Save(externalService.ToKernel());
        }
    }
}
