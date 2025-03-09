// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines the storage for replayed models.
/// </summary>
public interface IReplayedModelsStorage
{
    /// <summary>
    /// Store the fact that a model has been replayed.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>Awaitable task.</returns>
    Task Replayed(ReplayContext context);
}
