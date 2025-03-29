// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Models;

namespace Cratis.Chronicle.Storage.Sinks;

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
    /// <param name="model">The <see cref="ModelName"/> to get the context for.</param>
    /// <returns><see cref="Result"/> of either the <see cref="ReplayContext"/> or an error.</returns>
    Task<Result<ReplayContext, GetContextError>> TryGet(ModelName model);

    /// <summary>
    /// Remove a replay context.
    /// </summary>
    /// <param name="model">The <see cref="ModelName"/> to get the context for.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(ModelName model);
}
