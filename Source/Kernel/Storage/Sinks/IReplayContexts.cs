// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines a system for working with <see cref="ReplayContext"/>.
/// </summary>
public interface IReplayContexts
{
    /// <summary>
    /// Establish a <see cref="ReplayContext"/> for a given <see cref="ReadModelDefinition"/> and <see cref="ObserverKey"/> .
    /// </summary>
    /// <param name="readModelIdentifier">The <see cref="ReadModelIdentifier"/> the context is for.</param>
    /// <param name="readModelName">The <see cref="ReadModelName"/> the context is for.</param>
    /// <returns>A <see cref="ReplayContext"/> for the model.</returns>
    Task<ReplayContext> Establish(ReadModelIdentifier readModelIdentifier, ReadModelName readModelName);

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
