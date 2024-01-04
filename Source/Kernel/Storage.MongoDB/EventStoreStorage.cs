// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.MongoDB.EventTypes;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.Cratis.Kernel.Storage.Identities;
using Aksio.Cratis.Kernel.Storage.MongoDB.Identities;
using Aksio.Cratis.Kernel.Storage.Projections;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreStorage"/> for MongoDB.
/// </summary>
public class EventStoreStorage : IEventStoreStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreStorage"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStore"/> the storage is for.</param>
    /// <param name="clusterDatabase"><see cref="IClusterDatabase"/> to use.</param>
    /// <param name="eventStoreDatabase"><see cref="IEventStoreDatabase"/> to use.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStoreStorage(
        EventStore eventStore,
        IClusterDatabase clusterDatabase,
        IEventStoreDatabase eventStoreDatabase,
        ILoggerFactory loggerFactory)
    {
        EventStore = eventStore;
        Identities = new IdentityStorage(clusterDatabase, loggerFactory.CreateLogger<IdentityStorage>());
        EventTypes = new EventTypesStorage(eventStore, eventStoreDatabase, loggerFactory.CreateLogger<EventTypesStorage>());
    }

    /// <inheritdoc/>
    public EventStore EventStore { get; }

    /// <inheritdoc/>
    public IIdentityStorage Identities { get; }

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes { get; }

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections => throw new NotImplementedException();

    /// <inheritdoc/>
    public IProjectionPipelineDefinitionsStorage ProjectionPipelines => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventStoreInstanceStorage GetInstance(TenantId tenantId) => throw new NotImplementedException();
}
