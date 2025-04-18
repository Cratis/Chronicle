// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a watcher for a projection.
/// </summary>
public interface IProjectionWatcher
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
/// Defines a watcher for a projection.
/// </summary>
/// <typeparam name="TModel">Type of model the watcher is for.</typeparam>
public interface IProjectionWatcher<TModel> : IProjectionWatcher
{
    /// <summary>
    /// Gets the observable for the projection.
    /// </summary>
    IObservable<ProjectionChangeset<TModel>> Observable { get; }
}
