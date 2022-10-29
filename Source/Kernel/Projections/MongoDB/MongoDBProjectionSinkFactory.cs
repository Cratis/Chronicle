// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;

namespace Aksio.Cratis.Events.Projections.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSinkFactory"/>.
/// </summary>
public class MongoDBProjectionSinkFactory : IProjectionSinkFactory
{
    readonly IMongoDBClientFactory _clientFactory;
    readonly IExpandoObjectConverter _expandoObjectConverter;
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
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public MongoDBProjectionSinkFactory(
        IExecutionContextManager executionContextManager,
        IMongoDBClientFactory clientFactory,
        IExpandoObjectConverter expandoObjectConverter,
        Storage configuration)
    {
        _clientFactory = clientFactory;
        _expandoObjectConverter = expandoObjectConverter;
        _executionContextManager = executionContextManager;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public IProjectionSink CreateFor(Model model) =>
        new MongoDBProjectionSink(
            model,
            _executionContextManager,
            _clientFactory,
            _expandoObjectConverter,
            _configuration);
}
