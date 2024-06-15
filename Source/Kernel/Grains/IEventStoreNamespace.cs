// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Observation.Reducers;
using Cratis.Chronicle.Grains.Projections;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Defines a namespace within an <see cref="IEventStore"/>.
/// </summary>
public interface IEventStoreNamespace
{
    /// <summary>
    /// Gets the name of the namespace.
    /// </summary>
    EventStoreNamespaceName Name { get; }

    /// <summary>
    /// Gets the <see cref="IEventStoreNamespaceStorage"/> for the namespace.
    /// </summary>
    IEventStoreNamespaceStorage Storage { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionManager"/>.
    /// </summary>
    IProjectionManager ProjectionManager { get; }

    /// <summary>
    /// Gets the <see cref="IReducerPipelines"/>.
    /// </summary>
    IReducerPipelines ReducerPipelines { get; }

    /// <summary>
    /// Gets the sinks for the namespace.
    /// </summary>
    ISinks Sinks { get; }
}
