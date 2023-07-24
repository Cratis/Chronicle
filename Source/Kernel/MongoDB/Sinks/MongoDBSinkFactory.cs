// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;
using Aksio.MongoDB;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
public class MongoDBSinkFactory : ISinkFactory
{
    readonly IMongoDBClientFactory _clientFactory;
    readonly IMongoDBConverterFactory _mongoDBConverterFactory;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly IExecutionContextManager _executionContextManager;
    readonly Storage _configuration;

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <summary>
    /// /// Initializes a new instance of the <see cref="MongoDBSinkFactory"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> to use.</param>
    /// <param name="mongoDBConverterFactory"><see cref="IMongoDBConverterFactory"/> for creating converters.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public MongoDBSinkFactory(
        IExecutionContextManager executionContextManager,
        IMongoDBClientFactory clientFactory,
        IMongoDBConverterFactory mongoDBConverterFactory,
        IExpandoObjectConverter expandoObjectConverter,
        Storage configuration)
    {
        _executionContextManager = executionContextManager;
        _clientFactory = clientFactory;
        _mongoDBConverterFactory = mongoDBConverterFactory;
        _expandoObjectConverter = expandoObjectConverter;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public ISink CreateFor(Model model) =>
        new MongoDBSink(
            model,
            _executionContextManager,
            _clientFactory,
            _mongoDBConverterFactory,
            _expandoObjectConverter,
            _configuration);
}
