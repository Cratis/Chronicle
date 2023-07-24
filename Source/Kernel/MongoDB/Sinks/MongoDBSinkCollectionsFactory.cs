// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Projections;
using Aksio.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// REpresents an implementation of <see cref="IMongoDBSinkCollectionsFactory"/>.
/// </summary>
public class MongoDBSinkCollectionsFactory : IMongoDBSinkCollectionsFactory
{
    readonly IMongoDBClientFactory _clientFactory;
    readonly Storage _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBSinkCollectionsFactory"/> class.
    /// </summary>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for creating MongoDB clients.</param>
    /// <param name="configuration"></param>
    public MongoDBSinkCollectionsFactory(
        IMongoDBClientFactory clientFactory,
        Storage configuration)
    {
        _clientFactory = clientFactory;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public IMongoDBSinkCollections CreateFor(MicroserviceId microserviceId, TenantId tenantId, Model model)
    {
        var readModelsConfig = _configuration.Microservices.Get(microserviceId).Tenants[tenantId.ToString()].Get(WellKnownStorageTypes.ReadModels);
        var url = new MongoUrl(readModelsConfig.ConnectionDetails.ToString());
        var client = _clientFactory.Create(url);
        var database = client.GetDatabase(url.DatabaseName);
        return new MongoDBSinkCollections(database, model);
    }
}
