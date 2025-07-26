// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a factory for creating <see cref="IProjectionWatcher{TReadModel}"/>.
/// </summary>
public interface IProjectionWatcherFactory
{
    /// <summary>
    /// Create a watcher for a specific model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
    /// <param name="stopped">Callback for when the watcher is stopped.</param>
    /// <returns>An instance of <see cref="IProjectionWatcher{TReadModel}"/>.</returns>
    IProjectionWatcher<TReadModel> Create<TReadModel>(Action stopped);
}
