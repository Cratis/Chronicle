// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a manager for <see cref="IReadModelsWatcher"/>.
/// </summary>
public interface IReadModelsWatcherManager
{
    /// <summary>
    /// Gets a watcher for a specific model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
    /// <returns>An instance of <see cref="IReadModelsWatcher{TReadModel}"/>.</returns>
    IReadModelsWatcher<TReadModel> GetWatcher<TReadModel>();
}
