// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents a retained operation result, which cannot act again on a later member incarnation.
/// </summary>
/// <param name="OperationId">The idempotency identity of the operation.</param>
/// <param name="EventTypeId">The requested member.</param>
/// <param name="Outcome">The final result.</param>
/// <param name="Incarnation">The removed incarnation, only for a successful retirement.</param>
/// <param name="DeclaringConnections">The connected declarations that refused retirement.</param>
public record ObserverEventTypeRetirementResult(
    Guid OperationId,
    EventTypeId EventTypeId,
    ObserverEventTypeRetirementOutcome Outcome,
    ObserverTypeSetRevision? Incarnation,
    IReadOnlyList<ConnectionId> DeclaringConnections);
