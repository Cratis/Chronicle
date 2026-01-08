// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a watcher for a read model.
/// </summary>
public interface IReadModelsWatcher
{
    /// <summary>
    /// Start the watcher.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop the watcher. Will be removed from the manager.
    /// </summary>
    void Stop();
}

/// <summary>
/// Defines a watcher for a read model.
/// </summary>
/// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
public interface IReadModelsWatcher<TReadModel> : IReadModelsWatcher
{
    /// <summary>
    /// Gets the observable for the read model.
    /// </summary>
    IObservable<ReadModelChangeset<TReadModel>> Observable { get; }
}
