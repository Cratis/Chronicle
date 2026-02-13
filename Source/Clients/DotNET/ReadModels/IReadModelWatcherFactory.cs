// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a factory for creating <see cref="IReadModelWatcher{TReadModel}"/>.
/// </summary>
public interface IReadModelWatcherFactory
{
    /// <summary>
    /// Create a watcher for a specific model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
    /// <param name="stopped">Callback for when the watcher is stopped.</param>
    /// <returns>An instance of <see cref="IReadModelWatcher{TReadModel}"/>.</returns>
    IReadModelWatcher<TReadModel> Create<TReadModel>(Action stopped);
}
