// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// Defines a storage system for working with <see cref="ReplayContext"/>.
/// </summary>
public interface IReplayContextsStorage
{
    /// <summary>
    /// Save a replay context.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> to save.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Save(ReplayContext context);

    /// <summary>
    /// Try to get the replay context for a given observer.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> to get the context for.</param>
    /// <returns><see cref="Result"/> of either the <see cref="ReplayContext"/> or an error.</returns>
    Task<Result<ReplayContext, GetContextError>> TryGet(ReadModelIdentifier readModel);

    /// <summary>
    /// Remove a replay context.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> to get the context for.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(ReadModelIdentifier readModel);
}
