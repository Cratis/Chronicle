// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a manager for <see cref="IProjectionWatcher"/>.
/// </summary>
public interface IProjectionWatcherManager
{
    /// <summary>
    /// Gets a watcher for a specific model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
    /// <returns>An instance of <see cref="IProjectionWatcher{TReadModel}"/>.</returns>
    IProjectionWatcher<TReadModel> GetWatcher<TReadModel>();
}
