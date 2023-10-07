// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Models;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Sinks;
using Aksio.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
public class MongoDBSinkFactory : ISinkFactory
{
    readonly ITypeFormats _typeFormats;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly IMongoDBClientFactory _clientFactory;
    readonly Storage _configuration;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// /// Initializes a new instance of the <see cref="MongoDBSinkFactory"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for creating MongoDB clients.</param>
    /// <param name="configuration">The <see cref="Storage"/> configuration.</param>
    public MongoDBSinkFactory(
        IExecutionContextManager executionContextManager,
        ITypeFormats typeFormats,
        IExpandoObjectConverter expandoObjectConverter,
        IMongoDBClientFactory clientFactory,
        Storage configuration)
    {
        _executionContextManager = executionContextManager;
        _typeFormats = typeFormats;
        _expandoObjectConverter = expandoObjectConverter;
        _clientFactory = clientFactory;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public ISink CreateFor(Model model)
    {
        var executionContext = _executionContextManager.Current;

        var readModelsConfig = _configuration.Microservices.Get(executionContext.MicroserviceId).Tenants[executionContext.TenantId.ToString()].Get(WellKnownStorageTypes.ReadModels);
        var url = new MongoUrl(readModelsConfig.ConnectionDetails.ToString());
        var client = _clientFactory.Create(url);
        var database = client.GetDatabase(url.DatabaseName);

        var mongoDBConverter = new MongoDBConverter(
                _expandoObjectConverter,
                _typeFormats,
                model);
        var collections = new MongoDBSinkCollections(database, model);
        var changesetConverter = new MongoDBChangesetConverter(model, mongoDBConverter, collections, _expandoObjectConverter);

        return new MongoDBSink(
            model,
            mongoDBConverter,
            collections,
            changesetConverter,
            _expandoObjectConverter);
    }
}
