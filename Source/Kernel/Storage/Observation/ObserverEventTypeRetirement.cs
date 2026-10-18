// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents an operator's pending request to remove an event-type member across generations.
/// </summary>
/// <param name="OperationId">The idempotency identity of the operation.</param>
/// <param name="EventTypeId">The member to retire.</param>
public record ObserverEventTypeRetirement(Guid OperationId, EventTypeId EventTypeId);
