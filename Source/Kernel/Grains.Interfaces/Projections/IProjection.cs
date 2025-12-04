// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a projection.
/// </summary>
public interface IProjection : IGrainWithStringKey
{
    /// <summary>
    /// Set the projection definition and subscribe as an observer.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to refresh with.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinition(ProjectionDefinition definition);

    /// <summary>
    /// Get the projection definition.
    /// </summary>
    /// <returns>The current <see cref="ProjectionDefinition"/>.</returns>
    Task<ProjectionDefinition> GetDefinition();

    /// <summary>
    /// Subscribe to changes in projection or pipeline definition changes.
    /// </summary>
    /// <param name="subscriber"><see cref="INotifyProjectionDefinitionsChanged"/> to subscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task SubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber);

    /// <summary>
    /// Unsubscribe to changes in projection or pipeline definition changes.
    /// </summary>
    /// <param name="subscriber"><see cref="INotifyProjectionDefinitionsChanged"/> to subscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task UnsubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber);

    /// <summary>
    /// Get all pending futures for a given key that need resolution.
    /// </summary>
    /// <param name="namespace">The namespace to get futures for.</param>
    /// <param name="key">The root key to get futures for.</param>
    /// <returns>Collection of <see cref="ProjectionFuture"/> instances.</returns>
    Task<IEnumerable<ProjectionFuture>> GetFutures(EventStoreNamespaceName @namespace, Key key);

    /// <summary>
    /// Add a future that needs resolution.
    /// </summary>
    /// <param name="namespace">The namespace to add the future to.</param>
    /// <param name="key">The root key this future is associated with.</param>
    /// <param name="future">The <see cref="ProjectionFuture"/> to add.</param>
    /// <returns>Awaitable task.</returns>
    Task AddFuture(EventStoreNamespaceName @namespace, Key key, ProjectionFuture future);

    /// <summary>
    /// Resolve a future that has been successfully resolved.
    /// </summary>
    /// <param name="namespace">The namespace to resolve the future in.</param>
    /// <param name="key">The root key.</param>
    /// <param name="futureId">The identifier of the future to resolve.</param>
    /// <returns>Awaitable task.</returns>
    Task ResolveFuture(EventStoreNamespaceName @namespace, Key key, ProjectionFutureId futureId);
}
