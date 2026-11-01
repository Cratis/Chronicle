// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents the result of requesting event-type retirement.
/// </summary>
/// <param name="OperationId">The operation identity, empty when no operation was started.</param>
/// <param name="Outcome">The operation outcome.</param>
public record RetireEventTypeFromObserverResult(Guid OperationId, ObserverTypeSetOperationOutcome Outcome);
