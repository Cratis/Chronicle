// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines a system for working with <see cref="ReplayContext"/>.
/// </summary>
public interface IReplayContexts
{
    /// <summary>
    /// Establish a <see cref="ReplayContext"/> for a given <see cref="Model"/> and <see cref="ObserverKey"/> .
    /// </summary>
    /// <param name="model">The <see cref="ModelName"/> the context is for.</param>
    /// <returns>A <see cref="ReplayContext"/> for the model.</returns>
    Task<ReplayContext> Establish(ModelName model);

    /// <summary>
    /// Establish a <see cref="ReplayContext"/> for a given <see cref="Model"/>.
    /// </summary>
    /// <param name="model">The <see cref="ModelName"/> the context is for.</param>
    /// <returns>A <see cref="ReplayContext"/> for the model.</returns>
    Task<Result<ReplayContext, GetContextError>> TryGet(ModelName model);

    /// <summary>
    /// Evict a <see cref="ReplayContext"/> for a given <see cref="ObserverKey"/>.
    /// </summary>
    /// <param name="model">The <see cref="ModelName"/> the context is for.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Evict(ModelName model);
}
