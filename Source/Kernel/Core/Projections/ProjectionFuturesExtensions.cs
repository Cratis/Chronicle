// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Extensions for <see cref="IProjectionFutures"/> to simplify usage by providing methods that include the necessary keys for accessing the grain.
/// </summary>
public static class ProjectionFuturesExtensions
{
    /// <summary>
    /// Get the <see cref="IProjectionFutures"/> grain for the specified event store, namespace, and projection identifier.
    /// </summary>
    /// <param name="grainFactory">The grain factory to use for getting the grain.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <param name="projectionId">The projection identifier.</param>
    /// <returns>The <see cref="IProjectionFutures"/> grain instance.</returns>
    public static IProjectionFutures GetProjectionFutures(this IGrainFactory grainFactory, EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId) =>
        grainFactory.GetGrain<IProjectionFutures>(new ProjectionFuturesKey(projectionId, eventStore, @namespace));
}
