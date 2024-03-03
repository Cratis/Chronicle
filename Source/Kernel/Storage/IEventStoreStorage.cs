// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Storage.EventTypes;
using Cratis.Kernel.Storage.Identities;
using Cratis.Kernel.Storage.Projections;

namespace Cratis.Kernel.Storage;

/// <summary>
/// Defines the shared storage for an event store.
/// </summary>
public interface IEventStoreStorage
{
    /// <summary>
    /// Gets the event store the storage represents.
    /// </summary>
    EventStoreName EventStore { get; }

    /// <summary>
    /// Gets the <see cref="IIdentityStorage"/> for the event store.
    /// </summary>
    IIdentityStorage Identities { get; }

    /// <summary>
    /// Gets the <see cref="IEventTypesStorage"/> for the event store.
    /// </summary>
    IEventTypesStorage EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionDefinitionsStorage"/> for the event store.
    /// </summary>
    IProjectionDefinitionsStorage Projections { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionPipelineDefinitionsStorage"/> for the event store.
    /// </summary>
    IProjectionPipelineDefinitionsStorage ProjectionPipelines { get; }

    /// <summary>
    /// Get a specific <see cref="IEventStoreNamespaceStorage"/> for a <see cref="TenantId"/>.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to get for.</param>
    /// <returns>The <see cref="IEventStoreNamespaceStorage"/> instance.</returns>
    IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace);
}
