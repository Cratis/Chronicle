// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a factory for creating <see cref="IProjectionWatcher{TModel}"/>.
/// </summary>
public interface IProjectionWatcherFactory
{
    /// <summary>
    /// Create a watcher for a specific model.
    /// </summary>
    /// <typeparam name="TModel">Type of model the watcher is for.</typeparam>
    /// <param name="stopped">Callback for when the watcher is stopped.</param>
    /// <returns>An instance of <see cref="IProjectionWatcher{TModel}"/>.</returns>
    IProjectionWatcher<TModel> Create<TModel>(Action stopped);
}
