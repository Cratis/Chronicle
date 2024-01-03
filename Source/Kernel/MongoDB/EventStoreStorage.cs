// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.MongoDB.Identities;
using Aksio.Cratis.Kernel.Persistence;
using Aksio.Cratis.Kernel.Persistence.Identities;
using Aksio.Cratis.Kernel.Persistence.Projections;
using Aksio.Cratis.Kernel.Persistence.Schemas;
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
    /// <param name="clusterDatabase"><see cref="IClusterDatabase"/> to use.</param>
    /// <param name="eventStoreDatabase"><see cref="IEventStoreDatabase"/> to use.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStoreStorage(
        IClusterDatabase clusterDatabase,
        IEventStoreDatabase eventStoreDatabase,
        ILoggerFactory loggerFactory)
    {
        Identities = new MongoDBIdentityStorage(clusterDatabase, loggerFactory.CreateLogger<MongoDBIdentityStorage>());
    }

    /// <inheritdoc/>
    public IIdentityStorage Identities { get; }

    /// <inheritdoc/>
    public ISchemaStore Schemas => throw new NotImplementedException();

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections => throw new NotImplementedException();

    /// <inheritdoc/>
    public IProjectionPipelineDefinitionsStorage ProjectionPipelines => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventStoreInstanceStorage GetInstance(TenantId tenantId) => throw new NotImplementedException();
}
