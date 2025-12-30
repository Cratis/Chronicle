// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Defines a watcher for a reducer.
/// </summary>
public interface IReducerWatcher
{
    /// <summary>
    /// Start the watcher.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop the watcher.
    /// </summary>
    void Stop();
}

/// <summary>
/// Defines a watcher for a reducer.
/// </summary>
/// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
public interface IReducerWatcher<TReadModel> : IReducerWatcher
{
    /// <summary>
    /// Gets the observable for the reducer.
    /// </summary>
    IObservable<ReducerChangeset<TReadModel>> Observable { get; }
}
