// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Requests completion of an observer definition change.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="Namespace">The namespace name.</param>
/// <param name="ObserverId">The observer identifier.</param>
/// <param name="EventSequenceId">The event sequence identifier.</param>
/// <param name="Reason">The operator's reason for completing the change.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.ObserverTypeSets)]
public record CompleteDefinitionChange(string EventStore, string Namespace, string ObserverId, string EventSequenceId, string Reason)
{
    /// <summary>
    /// Reports availability without changing observer state.
    /// </summary>
    /// <param name="storage">The event store storage.</param>
    /// <returns>The availability of the operation.</returns>
    public async Task<CompleteDefinitionChangeResult> Handle(IStorage storage) => new(
        Guid.Empty,
        await ObserverTypeSetAvailability.Get(EventStore, storage));
}
