// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBClientFactory"/>.
/// </summary>
[Singleton]
public class MongoDBClientFactory : IMongoDBClientFactory
{
    readonly ILogger<MongoDBClientFactory> _logger;
    readonly ConcurrentDictionary<string, IMongoClient> _clients = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBClientFactory"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public MongoDBClientFactory(ILogger<MongoDBClientFactory> logger) => _logger = logger;

    /// <inheritdoc/>
    public IMongoClient Create(MongoClientSettings settings) => _clients.GetOrAdd(settings.Server.ToString(), (_, v) => v, CreateImplementation(settings));

    /// <inheritdoc/>
    public IMongoClient Create(MongoUrl url) => Create(MongoClientSettings.FromUrl(url));

    /// <inheritdoc/>
    public IMongoClient Create(string connectionString) => Create(MongoClientSettings.FromConnectionString(connectionString));

    IMongoClient CreateImplementation(MongoClientSettings settings)
    {
        settings.ClusterConfigurator = ClusterConfigurator;
        _logger.CreateClient(settings.Server.ToString());
        return new MongoClient(settings);
    }

    void ClusterConfigurator(ClusterBuilder builder)
    {
        builder
            .Subscribe<CommandStartedEvent>(command => _logger.CommandStarted(command.RequestId, command.CommandName, command.Command.ToJson()))
            .Subscribe<CommandFailedEvent>(command => _logger.CommandFailed(command.RequestId, command.CommandName, command.Failure.Message))
            .Subscribe<CommandSucceededEvent>(command => _logger.CommandSucceeded(command.RequestId, command.CommandName));
    }
}
