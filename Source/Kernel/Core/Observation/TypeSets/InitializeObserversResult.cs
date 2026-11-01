// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents the result of initializing an event store's observers.
/// </summary>
/// <param name="Outcome">The operation outcome.</param>
/// <param name="Observers">The individual observer results.</param>
/// <param name="Reasons">The event-store refusal reasons.</param>
public record InitializeObserversResult(ObserverTypeSetOperationOutcome Outcome, IEnumerable<ObserverInitializationResult> Observers, IEnumerable<string> Reasons);
