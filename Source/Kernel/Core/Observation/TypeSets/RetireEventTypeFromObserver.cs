// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Requests retirement of an event type from an observer across generations.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="Namespace">The namespace, or null for all namespaces.</param>
/// <param name="ObserverId">The observer identifier.</param>
/// <param name="EventSequenceId">The event sequence identifier.</param>
/// <param name="EventTypeId">The event type identifier to retire.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.ObserverTypeSets)]
public record RetireEventTypeFromObserver(string EventStore, string? Namespace, string ObserverId, string EventSequenceId, string EventTypeId)
{
    /// <summary>
    /// Reports availability without changing observer state.
    /// </summary>
    /// <param name="storage">The event store storage.</param>
    /// <returns>The operation identity and availability.</returns>
    public async Task<RetireEventTypeFromObserverResult> Handle(IStorage storage) => new(
        Guid.Empty,
        await ObserverTypeSetAvailability.Get(EventStore, storage));
}
