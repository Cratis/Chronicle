// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents one observer's initialization result.
/// </summary>
/// <param name="Namespace">The namespace name.</param>
/// <param name="ObserverId">The observer identifier.</param>
/// <param name="State">The initialization state.</param>
/// <param name="RefusalReasons">The reasons initialization was refused.</param>
public record ObserverInitializationResult(string Namespace, string ObserverId, ObserverInitializationState State, IEnumerable<string> RefusalReasons);
