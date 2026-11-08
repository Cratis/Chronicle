// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="INeo4jConnections"/>.
/// </summary>
/// <remarks>
/// Resolves through <see cref="Configuration.Storage.ConnectionResolution"/> with the storage <see cref="Neo4jConnectionStorage.Name"/>,
/// the scope kind <c language="text">readModels</c>, and the event store and namespace of the graph. A resolved connection string replaces
/// <see cref="Neo4jOptions.Server"/>, a resolved database replaces the database name Chronicle derives, and a resolved
/// identity replaces the configured credentials. Without any resolution configured, the configured server and credentials
/// are used with the derived database name.
/// </remarks>
[Singleton]
public class Neo4jConnections : INeo4jConnections
{
    readonly IOptions<ChronicleOptions> _options;
    readonly IConfiguration _configuration;
    readonly INeo4jDriverFactory _driverFactory;
    readonly IStorageConnectionResolution _resolution;
    readonly ConcurrentDictionary<string, Neo4jConnectionResolutionHooks> _hooks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jConnections"/> class.
    /// </summary>
    /// <param name="options">The <see cref="IOptions{TOptions}"/> of <see cref="ChronicleOptions"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> the password secret is read from.</param>
    /// <param name="driverFactory">The <see cref="INeo4jDriverFactory"/> creating the drivers.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get the connection resolver factory from.</param>
    public Neo4jConnections(
        IOptions<ChronicleOptions> options,
        IConfiguration configuration,
        INeo4jDriverFactory driverFactory,
        IServiceProvider serviceProvider)
        : this(
            options,
            configuration,
            driverFactory,
            new StorageConnectionResolution(
                Neo4jConnectionStorage.Name,
                options.Value.Storage.ConnectionResolution,
                serviceProvider.GetService<IConnectionResolverFactory>() ?? new ConnectionResolverFactory(serviceProvider)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jConnections"/> class with an explicit resolution.
    /// </summary>
    /// <param name="options">The <see cref="IOptions{TOptions}"/> of <see cref="ChronicleOptions"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> the password secret is read from.</param>
    /// <param name="driverFactory">The <see cref="INeo4jDriverFactory"/> creating the drivers.</param>
    /// <param name="resolution">The <see cref="IStorageConnectionResolution"/> for the storage <see cref="Neo4jConnectionStorage.Name"/>.</param>
    internal Neo4jConnections(
        IOptions<ChronicleOptions> options,
        IConfiguration configuration,
        INeo4jDriverFactory driverFactory,
        IStorageConnectionResolution resolution)
    {
        _options = options;
        _configuration = configuration;
        _driverFactory = driverFactory;
        _resolution = resolution;
    }

    /// <inheritdoc/>
    public Neo4jOptions Options => _options.Value.Neo4j;

    /// <inheritdoc/>
    public async ValueTask<Neo4jConnection> Resolve(EventStoreName eventStore, EventStoreNamespaceName @namespace, ConnectionPurpose purpose)
    {
        var options = Options;
        var configured = GetConfiguredAuthentication(options);
        var resolved = ResolvedConnection.Unchanged;
        Neo4jConnectionResolutionHooks? hooks = null;
        if (_resolution.IsConfigured)
        {
            var request = _resolution.RequestFor(purpose, StorageConnectionKind.ReadModels, eventStore.Value, @namespace.Value);
            resolved = await _resolution.Resolve(purpose, StorageConnectionKind.ReadModels, eventStore.Value, @namespace.Value);
            (ICachingConnectionResolver Resolver, ConnectionRequest Request, ConfiguredNeo4jAuthentication Configured) state = (_resolution.Resolver, request, configured);
            hooks = _hooks.GetOrAdd(request.ToKey(), static (_, arguments) => new(arguments.Resolver, arguments.Request, arguments.Configured), state);
        }

        var server = string.IsNullOrEmpty(resolved.ConnectionString) ? options.Server : resolved.ConnectionString;
        if (string.IsNullOrEmpty(server))
        {
            throw new MissingNeo4jServer(eventStore, @namespace);
        }

        var driver = _driverFactory.Create(new Uri(server), (IAuthTokenManager?)hooks ?? configured);
        var database = DatabaseFor(options, resolved.Database, eventStore, @namespace);
        return new(driver, database, resolved.Database is null && options.DatabaseMode == Neo4jDatabaseMode.PerNamespace && options.CreateIfMissing);
    }

    static string DatabaseFor(Neo4jOptions options, string? resolved, EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (!string.IsNullOrEmpty(resolved))
        {
            return resolved;
        }

        return options.DatabaseMode == Neo4jDatabaseMode.Single
            ? options.SingleDatabase
            : GraphDatabaseName.For(options.DatabasePrefix, eventStore.Value, @namespace.Value);
    }

    ConfiguredNeo4jAuthentication GetConfiguredAuthentication(Neo4jOptions options)
    {
        var password = string.IsNullOrEmpty(options.PasswordSecret) ? null : _configuration[options.PasswordSecret];
        return new(options.UserName, password);
    }
}
