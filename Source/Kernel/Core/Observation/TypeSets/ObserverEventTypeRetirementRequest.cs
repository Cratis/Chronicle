// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents a pending request to retire an event-type member.
/// </summary>
/// <param name="OperationId">The operation identity.</param>
/// <param name="EventTypeId">The member to retire.</param>
public record ObserverEventTypeRetirementRequest(Guid OperationId, string EventTypeId);
