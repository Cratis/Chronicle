// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Defines a system for managing observers of reducer changes.
/// </summary>
public interface IReducerObservers
{
    /// <summary>
    /// Get a watcher for a specific read model type.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
    /// <returns>An instance of <see cref="IReducerWatcher{TReadModel}"/>.</returns>
    IReducerWatcher<TReadModel> GetWatcher<TReadModel>();

    /// <summary>
    /// Notify observers that a reducer has produced a change.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model.</typeparam>
    /// <param name="namespace">The namespace for the event store.</param>
    /// <param name="modelKey">The <see cref="ReadModelKey"/> for the model.</param>
    /// <param name="readModel">The instance of the read model.</param>
    /// <param name="removed">Whether the read model was removed.</param>
    void NotifyChange<TReadModel>(EventStoreNamespaceName @namespace, ReadModelKey modelKey, TReadModel? readModel, bool removed);
}
