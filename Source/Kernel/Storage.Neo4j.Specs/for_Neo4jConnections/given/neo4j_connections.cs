// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jConnections.given;

public class neo4j_connections : Specification
{
    protected const string SecretKey = "Neo4j:Password";

    protected Neo4jOptions _neo4j;
    protected ConnectionResolutionOptions _resolutionOptions;
    protected ICachingConnectionResolver _resolver;
    protected ConnectionRequest? _request;
    protected ResolvedConnection _resolved;
    protected INeo4jDriverFactory _driverFactory;
    protected IDriver _driver;
    protected Uri _server;
    protected IAuthTokenManager _authentication;
    protected Neo4jConnections _connections;

    protected virtual Neo4jOptions CreateOptions() => new() { Server = "bolt://configured:7687", UserName = "chronicle", PasswordSecret = SecretKey };

    protected void Configure() => _connections = CreateConnections();

    protected Neo4jConnections CreateConnections()
    {
        var options = Options.Create(new ChronicleOptions
        {
            Neo4j = _neo4j,
            Storage = new() { ConnectionResolution = _resolutionOptions }
        });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [SecretKey] = "the-secret" }).Build();
        var factory = Substitute.For<IConnectionResolverFactory>();
        factory.Create(Arg.Any<ConnectionResolutionOptions>()).Returns(_resolver);
        var resolution = new StorageConnectionResolution(Neo4jConnectionStorage.Name, _resolutionOptions, factory);
        return new(options, configuration, _driverFactory, resolution);
    }

    void Establish()
    {
        _neo4j = CreateOptions();
        _resolutionOptions = new();
        _resolved = ResolvedConnection.Unchanged;
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _resolver.Resolve(Arg.Do<ConnectionRequest>(_ => _request = _), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(_resolved));
        _driver = Substitute.For<IDriver>();
        _driverFactory = Substitute.For<INeo4jDriverFactory>();
        _driverFactory.Create(Arg.Do<Uri>(_ => _server = _), Arg.Do<IAuthTokenManager>(_ => _authentication = _)).Returns(_driver);
    }
}
