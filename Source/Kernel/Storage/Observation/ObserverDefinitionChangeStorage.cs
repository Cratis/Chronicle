// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The four independently paged row families of decision 0011. Terminal outcomes live in inventory rows,
/// never in a second completion document that could disagree with admission's inventory read.
/// </summary>
/// <param name="Progress">Durable step checkpoints and outcomes.</param>
/// <param name="Continuations">Partition catch-up successor obligations.</param>
/// <param name="RestartPlans">Replay successor obligations.</param>
/// <param name="Inventory">Registered jobs, including their absorbing terminal outcomes.</param>
public record ObserverDefinitionChangeStorage(
    IObserverDefinitionChangeRows<ObserverProgressRecord> Progress,
    IObserverDefinitionChangeRows<ObserverContinuation> Continuations,
    IObserverDefinitionChangeRows<ObserverRestartPlan> RestartPlans,
    IObserverDefinitionChangeRows<ObserverJobInventory> Inventory);
