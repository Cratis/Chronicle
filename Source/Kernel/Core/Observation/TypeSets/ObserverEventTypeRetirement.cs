// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents a retained final event-type retirement result.
/// </summary>
/// <param name="OperationId">The operation identity.</param>
/// <param name="EventTypeId">The requested member.</param>
/// <param name="Outcome">The final outcome.</param>
/// <param name="Incarnation">The removed incarnation, only for a successful retirement.</param>
/// <param name="DeclaringConnections">The connections that refused retirement.</param>
[ReadModel]
[BelongsTo(WellKnownServices.ObserverTypeSets)]
public record ObserverEventTypeRetirement(Guid OperationId, string EventTypeId, ObserverEventTypeRetirementOutcome Outcome, ObserverTypeSetIncarnation? Incarnation, IEnumerable<string> DeclaringConnections)
{
    /// <summary>
    /// Gets the result for an operation, if known. This kernel cannot start retirement operations.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="operationId">The operation identity.</param>
    /// <param name="storage">The event store storage.</param>
    /// <returns>An empty collection until retirement orchestration is available.</returns>
    public static IEnumerable<ObserverEventTypeRetirement> GetByOperation(string eventStore, Guid operationId, IStorage storage)
    {
        // Preserve the query contract while retirement orchestration is unavailable.
        _ = eventStore;
        _ = operationId;
        _ = storage;
        return [];
    }
}
