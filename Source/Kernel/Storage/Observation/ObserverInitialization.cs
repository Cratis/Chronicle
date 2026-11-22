// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The initialization state and every refusal reason from the latest attempt, absent on legacy observers.
/// </summary>
/// <param name="State">The durable initialization state.</param>
/// <param name="RefusalReasons">The latest attempt's reasons, including job or unsettled-handover details.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.ObserverInitialization")]
public record ObserverInitialization([Id(0)] ObserverInitializationState State, [Id(1)] IReadOnlyList<string> RefusalReasons);
