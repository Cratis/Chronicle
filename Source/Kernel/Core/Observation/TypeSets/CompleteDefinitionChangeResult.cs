// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents the result of requesting completion of a definition change.
/// </summary>
/// <param name="OperationId">The operation identity, empty when no operation was started.</param>
/// <param name="Outcome">The operation outcome.</param>
public record CompleteDefinitionChangeResult(Guid OperationId, ObserverTypeSetOperationOutcome Outcome);
