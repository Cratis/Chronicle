// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Integration;
using Cratis.Chronicle.Storage.Neo4j;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Chronicle.Neo4j.Integration;

/// <summary>
/// Lightweight base class for Neo4j integration specs that starts neither a kernel nor a silo.
/// Provides BDD lifecycle (Establish → Because → Cleanup/Destroy) and the connections the Neo4j storage resolves against the server of the fixture.
/// </summary>
/// <remarks>
/// Every specification gets a namespace of its own, so specifications that share the server never see each other's graphs. What a specification
/// leaves behind is removed when it ends: the nodes of its namespaces in single database mode, the databases of its namespaces otherwise.
/// What the specifications read back they read with Cypher of their own, through a driver that is not the one the storage under test uses.
/// </remarks>
/// <param name="fixture">The <see cref="Neo4jFixture"/> providing the Neo4j server.</param>
public abstract class Neo4jSpecification(Neo4jFixture fixture) : IAsyncLifetime, IDisposable
{
    /// <summary>
    /// The name of the event store the specifications use.
    /// </summary>
    public const string EventStore = "specs";

    /// <summary>
    /// The name of the configuration key the password of the server is read from.
    /// </summary>
    public const string PasswordKey = "NEO4J_PASSWORD";

    readonly Neo4jDriverFactory _driverFactory = new();
    readonly List<string> _namespaces = [];
    readonly List<string> _databases = [];
    Neo4jConnections? _connections;

    /// <summary>
    /// Gets the namespace of the specification, which no other specification uses.
    /// </summary>
    public string Namespace { get; } = NewNamespace();

    /// <summary>
    /// Gets the <see cref="Neo4jFixture"/>.
    /// </summary>
    protected Neo4jFixture Fixture => fixture;

    /// <summary>
    /// Gets the Neo4j server.
    /// </summary>
    protected Neo4jGraphServer Server => fixture.Server;

    /// <summary>
    /// Gets a value indicating whether every namespace shares one database.
    /// </summary>
    public bool IsSingleDatabase => fixture.IsSingleDatabase;

    /// <summary>
    /// Gets the connections to the server, with the options of <see cref="CreateOptions"/>.
    /// </summary>
    protected Neo4jConnections Connections => _connections ??= CreateConnections(CreateOptions());

    /// <summary>
    /// Create a namespace name no other specification uses.
    /// </summary>
    /// <returns>The namespace name.</returns>
    public static string NewNamespace() => $"n{Guid.NewGuid():N}";

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await InvokeChain("Establish", baseFirst: true);
        await InvokeChain("Because", baseFirst: true);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await InvokeChain("Cleanup", baseFirst: false);
        await InvokeChain("Destroy", baseFirst: false);
        await RemoveWhatWasWritten();
        await _driverFactory.DisposeAsync();
    }

    /// <inheritdoc/>
    public virtual void Dispose() => GC.SuppressFinalize(this);

    /// <summary>
    /// Create the options the connections are made with: the server of the fixture, the generated password by name, and the database mode of the fixture.
    /// </summary>
    /// <returns>The <see cref="Neo4jOptions"/>.</returns>
    protected virtual Neo4jOptions CreateOptions() => new()
    {
        Server = Server.HostUri,
        UserName = Neo4jGraphServer.UserName,
        PasswordSecret = PasswordKey,
        DatabaseMode = fixture.DatabaseMode
    };

    /// <summary>
    /// Create connections to the server.
    /// </summary>
    /// <param name="options">The <see cref="Neo4jOptions"/>.</param>
    /// <param name="password">The password the configuration holds for <see cref="PasswordKey"/>. Defaults to the one of the server.</param>
    /// <returns>The <see cref="Neo4jConnections"/>, which share their drivers with every other connections of the specification.</returns>
    protected Neo4jConnections CreateConnections(Neo4jOptions options, string? password = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [PasswordKey] = password ?? Server.Password })
            .Build();
        return new(Options.Create(new Cratis.Chronicle.Configuration.ChronicleOptions { Neo4j = options }), configuration, _driverFactory, new ServiceCollection().BuildServiceProvider());
    }

    /// <summary>
    /// Create a sink on the connections of the specification.
    /// </summary>
    /// <param name="definition">The <see cref="GraphDefinition"/> of the graph. Defaults to the organization graph.</param>
    /// <param name="namespace">The namespace of the graph. Defaults to the namespace of the specification.</param>
    /// <param name="connections">The connections to use. Defaults to the connections of the specification.</param>
    /// <returns>The <see cref="Neo4jGraphSink"/>.</returns>
    protected Neo4jGraphSink CreateSink(GraphDefinition? definition = null, string? @namespace = null, INeo4jConnections? connections = null)
    {
        var name = @namespace ?? Namespace;
        Track(name);
        return new(definition ?? GraphDefinitions.Organization, EventStore, name, connections ?? Connections);
    }

    /// <summary>
    /// Remember a namespace the specification writes in, so what is written there is removed when the specification ends.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    protected void Track(string @namespace)
    {
        if (!_namespaces.Contains(@namespace)) _namespaces.Add(@namespace);
    }

    /// <summary>
    /// Remember a database the specification creates, so it is dropped when the specification ends.
    /// </summary>
    /// <param name="database">The name of the database.</param>
    protected void TrackDatabase(string database) => _databases.Add(database);

    /// <summary>
    /// Get the name of the database a namespace lives in, as the fixture's database mode places it.
    /// </summary>
    /// <param name="namespace">The namespace. Defaults to the namespace of the specification.</param>
    /// <returns>The name of the database.</returns>
    protected string DatabaseOf(string? @namespace = null) =>
        IsSingleDatabase ? Neo4jGraphServer.Database : GraphDatabaseName.For(null, EventStore, @namespace ?? Namespace);

    /// <summary>
    /// Run Cypher against the database of a namespace, through the driver of the fixture, and return what it returns.
    /// </summary>
    /// <param name="cypher">The Cypher text.</param>
    /// <param name="parameters">The parameters of the statement.</param>
    /// <param name="namespace">The namespace whose database is read. Defaults to the namespace of the specification.</param>
    /// <returns>The records.</returns>
    protected async Task<IReadOnlyList<IRecord>> Read(string cypher, IDictionary<string, object>? parameters = null, string? @namespace = null) =>
        await Run(DatabaseOf(@namespace), cypher, parameters);

    /// <summary>
    /// Run Cypher against a database, through the driver of the fixture, and return what it returns.
    /// </summary>
    /// <param name="database">The name of the database.</param>
    /// <param name="cypher">The Cypher text.</param>
    /// <param name="parameters">The parameters of the statement.</param>
    /// <returns>The records.</returns>
    protected async Task<IReadOnlyList<IRecord>> Run(string database, string cypher, IDictionary<string, object>? parameters = null)
    {
        await using var session = Server.Driver.AsyncSession(_ => _.WithDatabase(database));
        var cursor = await session.RunAsync(cypher, parameters ?? new Dictionary<string, object>());
        return await cursor.ToListAsync();
    }

    /// <summary>
    /// Get the names of the databases the server has.
    /// </summary>
    /// <returns>The names.</returns>
    protected async Task<IReadOnlyList<string>> Databases() =>
        [.. (await Run(Neo4jConnection.SystemDatabase, "SHOW DATABASES YIELD name RETURN name")).Select(_ => _["name"].As<string>())];

    async Task RemoveWhatWasWritten()
    {
        if (IsSingleDatabase)
        {
            await Run(
                Neo4jGraphServer.Database,
                "MATCH (n) WHERE n.`_namespace` IN $namespaces DETACH DELETE n",
                new Dictionary<string, object> { ["namespaces"] = _namespaces });
            return;
        }

        foreach (var database in _namespaces.Select(_ => GraphDatabaseName.For(null, EventStore, _)).Concat(_databases).Distinct())
        {
            await Run(Neo4jConnection.SystemDatabase, $"DROP DATABASE {GraphCypher.Escape(database)} IF EXISTS WAIT");
        }
    }

    async Task InvokeChain(string methodName, bool baseFirst)
    {
        var methods = CollectMethods(methodName);

        if (!baseFirst)
        {
            methods.Reverse();
        }

        var tasks = methods
            .Select(method => method.Invoke(this, null))
            .OfType<Task>();
        foreach (var task in tasks)
        {
            await task;
        }
    }

    List<MethodInfo> CollectMethods(string methodName)
    {
        var methods = new List<MethodInfo>();
        var type = GetType();

        while (type is not null && type != typeof(Neo4jSpecification))
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (method is not null)
            {
                methods.Insert(0, method);
            }

            type = type.BaseType;
        }

        return methods;
    }
}
