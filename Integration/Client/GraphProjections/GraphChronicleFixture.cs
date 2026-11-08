// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Neo4j;

namespace Cratis.Chronicle.Integration.GraphProjections;

/// <summary>
/// Represents the fixture of the graph projection specifications: the configurable Chronicle fixture with a Neo4j server beside it.
/// </summary>
/// <remarks>
/// <para>
/// The Neo4j server starts on the docker network of the fixture before the kernel container does, and both the kernel container and the
/// in-process silo get the connection to it through <c language="text">Cratis:Chronicle:Neo4j</c>. The password is passed as the
/// value of the configuration key the options name, and never written in the options.
/// </para>
/// <para>
/// The database mode is a parameter: <see cref="DatabaseMode"/> reads <c language="text">CHRONICLE_NEO4J_DATABASE_MODE</c>, and defaults to
/// <c language="text">Single</c>, the only mode the Community edition supports. A server with multiple databases runs the same specifications in
/// <c language="text">PerNamespace</c> mode by pointing <c language="text">CHRONICLE_NEO4J_IMAGE</c> at it. An Enterprise image is started under Neo4j's
/// Evaluation Agreement (see Cratis/Chronicle#5739); this repository ships no Enterprise image itself.
/// </para>
/// </remarks>
public class GraphChronicleFixture : ChronicleConfigurableFixture
{
    const string PasswordKey = "NEO4J_PASSWORD";
    const string ModeVariable = "CHRONICLE_NEO4J_DATABASE_MODE";

    Neo4jGraphServer? _graphServer;

    /// <summary>
    /// Gets the Neo4j database mode the sink runs in.
    /// </summary>
    public string DatabaseMode { get; } = Environment.GetEnvironmentVariable(ModeVariable) is { Length: > 0 } mode ? mode : "Single";

    /// <summary>
    /// Gets the name of the Neo4j database the graph of the default namespace lives in: the only database in single database mode, and the one the sink names
    /// for the event store and the default namespace in per namespace mode. A specification that uses another namespace asks the sink's naming for it.
    /// </summary>
    public string Database => DatabaseMode == "Single" ? Neo4jGraphServer.Database : GraphDatabaseName.For(null, Constants.EventStore, "Default");

    /// <summary>
    /// Gets the Neo4j server.
    /// </summary>
    /// <exception cref="Neo4jServerNotStarted">The server has not started.</exception>
    public Neo4jGraphServer GraphServer => _graphServer ?? throw new Neo4jServerNotStarted();

    /// <inheritdoc/>
    protected override IReadOnlyDictionary<string, string> KernelEnvironment => new Dictionary<string, string>
    {
        ["Cratis__Chronicle__Neo4j__Server"] = GraphServer.NetworkUri,
        ["Cratis__Chronicle__Neo4j__UserName"] = Neo4jGraphServer.UserName,
        ["Cratis__Chronicle__Neo4j__PasswordSecret"] = PasswordKey,
        ["Cratis__Chronicle__Neo4j__DatabaseMode"] = DatabaseMode,
        [PasswordKey] = GraphServer.Password
    };

    /// <summary>
    /// Gets the configuration the in-process silo is given, which connects it to the server the same way the kernel container is.
    /// </summary>
    public IReadOnlyDictionary<string, string?> HostConfiguration => new Dictionary<string, string?>
    {
        ["Cratis:Chronicle:Neo4j:Server"] = GraphServer.HostUri,
        ["Cratis:Chronicle:Neo4j:UserName"] = Neo4jGraphServer.UserName,
        ["Cratis:Chronicle:Neo4j:PasswordSecret"] = PasswordKey,
        ["Cratis:Chronicle:Neo4j:DatabaseMode"] = DatabaseMode,
        [PasswordKey] = GraphServer.Password
    };

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            if (_graphServer is not null)
            {
                await _graphServer.DisposeAsync();
            }
        }
    }

    /// <inheritdoc/>
    protected override DotNet.Testcontainers.Containers.IContainer BuildContainer(DotNet.Testcontainers.Networks.INetwork network)
    {
        _graphServer = Neo4jGraphServer.Start(network);
        return base.BuildContainer(network);
    }
}
