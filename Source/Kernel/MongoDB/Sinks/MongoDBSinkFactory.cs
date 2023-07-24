// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
public class MongoDBSinkFactory : ISinkFactory
{
    readonly IMongoDBConverterFactory _mongoDBConverterFactory;
    readonly IMongoDBSinkCollectionsFactory _mongoDBSinkCollectionsFactory;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly IExecutionContextManager _executionContextManager;

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <summary>
    /// /// Initializes a new instance of the <see cref="MongoDBSinkFactory"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="mongoDBConverterFactory"><see cref="IMongoDBConverterFactory"/> for creating converters.</param>
    /// <param name="mongoDBSinkCollectionsFactory"></param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    public MongoDBSinkFactory(
        IExecutionContextManager executionContextManager,
        IMongoDBConverterFactory mongoDBConverterFactory,
        IMongoDBSinkCollectionsFactory mongoDBSinkCollectionsFactory,
        IExpandoObjectConverter expandoObjectConverter)
    {
        _executionContextManager = executionContextManager;
        _mongoDBConverterFactory = mongoDBConverterFactory;
        _mongoDBSinkCollectionsFactory = mongoDBSinkCollectionsFactory;
        _expandoObjectConverter = expandoObjectConverter;
    }

    /// <inheritdoc/>
    public ISink CreateFor(Model model)
    {
        var executionContext = _executionContextManager.Current;
        return new MongoDBSink(
            model,
            _mongoDBConverterFactory.CreateFor(model),
            _mongoDBSinkCollectionsFactory.CreateFor(executionContext.MicroserviceId, executionContext.TenantId, model),
            _expandoObjectConverter);
    }
}
