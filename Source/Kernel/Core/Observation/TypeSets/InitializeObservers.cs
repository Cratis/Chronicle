// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Requests initialization of an event store's observer type sets.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="StopJobs">Whether to stop existing jobs.</param>
/// <param name="AcceptDuplicates">Whether duplicate delivery is accepted.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.ObserverTypeSets)]
public record InitializeObservers(string EventStore, bool StopJobs, bool AcceptDuplicates)
{
    /// <summary>
    /// Reports that this kernel cannot enable observer type sets.
    /// </summary>
    /// <returns>The unavailable result, without any storage operations.</returns>
    public InitializeObserversResult Handle() => new(ObserverTypeSetOperationOutcome.NotAvailable, [], []);
}
