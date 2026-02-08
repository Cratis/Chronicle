// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// Defines a system for working with <see cref="ReplayContext"/>.
/// </summary>
public interface IReplayContexts
{
    /// <summary>
    /// Establish a <see cref="ReplayContext"/> for a given <see cref="ReadModelDefinition"/> and <see cref="ObserverKey"/> .
    /// </summary>
    /// <param name="type">The <see cref="ReadModelType"/> of the read model being replayed.</param>
    /// <param name="containerName">The <see cref="ReadModelContainerName"/> the context is for.</param>
    /// <returns>A <see cref="ReplayContext"/> for the model.</returns>
    Task<ReplayContext> Establish(ReadModelType type, ReadModelContainerName containerName);

    /// <summary>
    /// Establish a <see cref="ReplayContext"/> for a given <see cref="ReadModelDefinition"/>.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> the context is for.</param>
    /// <returns>A <see cref="ReplayContext"/> for the model.</returns>
    Task<Result<ReplayContext, GetContextError>> TryGet(ReadModelIdentifier readModel);

    /// <summary>
    /// Evict a <see cref="ReplayContext"/> for a given <see cref="ObserverKey"/>.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> the context is for.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Evict(ReadModelIdentifier readModel);
}
