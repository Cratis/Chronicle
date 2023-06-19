// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Schemas;
using Aksio.MongoDB;

namespace Aksio.Cratis.Kernel.MongoDB.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSinkFactory"/>.
/// </summary>
public class MongoDBProjectionSinkFactory : IProjectionSinkFactory
{
    readonly IMongoDBClientFactory _clientFactory;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ITypeFormats _typeFormats;
    readonly IExecutionContextManager _executionContextManager;
    readonly Storage _configuration;

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.MongoDB;

    /// <summary>
    /// /// Initializes a new instance of the <see cref="MongoDBProjectionSinkFactory"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> to use.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public MongoDBProjectionSinkFactory(
        IExecutionContextManager executionContextManager,
        IMongoDBClientFactory clientFactory,
        IExpandoObjectConverter expandoObjectConverter,
        ITypeFormats typeFormats,
        Storage configuration)
    {
        _executionContextManager = executionContextManager;
        _clientFactory = clientFactory;
        _expandoObjectConverter = expandoObjectConverter;
        _typeFormats = typeFormats;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public IProjectionSink CreateFor(Model model) =>
        new MongoDBProjectionSink(
            model,
            _executionContextManager,
            _clientFactory,
            _expandoObjectConverter,
            _typeFormats,
            _configuration);
}
