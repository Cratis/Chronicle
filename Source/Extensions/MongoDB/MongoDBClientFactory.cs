// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Aksio.Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IMongoDBClientFactory"/>.
    /// </summary>
    [Singleton]
    public class MongoDBClientFactory : IMongoDBClientFactory
    {
        readonly ILogger<MongoDBClientFactory> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBClientFactory"/> class.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/> for logging.</param>
        public MongoDBClientFactory(ILogger<MongoDBClientFactory> logger) => _logger = logger;

        /// <inheritdoc/>
        public IMongoClient Create(MongoClientSettings settings)
        {
            settings.ClusterConfigurator = builder =>
            {
                builder
                    .Subscribe<CommandStartedEvent>(command => _logger.CommandStarted(command.RequestId, command.CommandName))
                    .Subscribe<CommandFailedEvent>(command => _logger.CommandFailed(command.RequestId, command.CommandName, command.Failure.Message))
                    .Subscribe<CommandSucceededEvent>(command => _logger.CommandSucceeded(command.RequestId, command.CommandName));
            };

            _logger.CreateClient(settings.Server.ToString());
            return new MongoClient(settings);
        }

        /// <inheritdoc/>
        public IMongoClient Create(MongoUrl url) => Create(MongoClientSettings.FromUrl(url));

        /// <inheritdoc/>
        public IMongoClient Create(string connectionString) => Create(MongoClientSettings.FromConnectionString(connectionString));
    }
}
