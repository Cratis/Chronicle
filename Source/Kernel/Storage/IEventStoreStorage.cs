// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.Cratis.Kernel.Storage.Identities;
using Aksio.Cratis.Kernel.Storage.Projections;

namespace Aksio.Cratis.Kernel.Storage;

/// <summary>
/// Defines the shared storage for an event store.
/// </summary>
public interface IEventStoreStorage
{
    /// <summary>
    /// Gets the event store the storage represents.
    /// </summary>
    EventStore EventStore { get; }

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
    /// Get a specific <see cref="IEventStoreInstanceStorage"/> for a <see cref="TenantId"/>.
    /// </summary>
    /// <param name="tenantId">The <see cref="TenantId"/> to get for.</param>
    /// <returns>The <see cref="IEventStoreInstanceStorage"/> instance.</returns>
    IEventStoreInstanceStorage GetInstance(TenantId tenantId);
}
