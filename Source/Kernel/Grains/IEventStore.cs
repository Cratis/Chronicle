// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Observation.Reducers;
using Aksio.Cratis.Kernel.Grains.Projections.Definitions;
using Aksio.Cratis.Kernel.Storage;

namespace Aksio.Cratis.Kernel.Grains;

/// <summary>
/// Defines an <see cref="IEventStore"/>.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Gets the name of the event store.
    /// </summary>
    EventStoreName Name { get; }

    /// <summary>
    /// Gets the <see cref="IEventStoreStorage"/> for the specific event store.
    /// </summary>
    IEventStoreStorage Storage { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionDefinitions"/>.
    /// </summary>
    IProjectionDefinitions ProjectionDefinitions { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionPipelineDefinitions"/>.
    /// </summary>
    IProjectionPipelineDefinitions ProjectionPipelineDefinitions { get; }

    /// <summary>
    /// Gets the <see cref="IReducerPipelineDefinitions"/>.
    /// </summary>
    IReducerPipelineDefinitions ReducerPipelineDefinitions { get; }

    /// <summary>
    /// Gets the <see cref="IImmutableList{T}"/> of <see cref="EventStoreNamespace"/> instances.
    /// </summary>
    IImmutableList<IEventStoreNamespace> Namespaces { get; }

    /// <summary>
    /// Get the <see cref="IEventStoreNamespace"/> for a specific <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to get for.</param>
    /// <returns><see cref="IEventStoreNamespace"/> for the specific <see cref="EventStoreNamespaceName"/>.</returns>
    IEventStoreNamespace GetNamespace(EventStoreNamespaceName @namespace);
}
