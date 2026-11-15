// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Integration;

namespace Cratis.Chronicle.Neo4j.Integration;

/// <summary>
/// Represents the fixture of the Neo4j integration specifications: a Neo4j server, and nothing else. There is no kernel and no silo,
/// the specifications use the Neo4j storage the way the kernel does.
/// </summary>
/// <remarks>
/// <para>
/// The image is <c language="text">neo4j:5-community</c> unless <c language="text">CHRONICLE_NEO4J_IMAGE</c> names another, and the sink runs in
/// <see cref="Neo4jDatabaseMode.Single"/> mode, the only mode the Community edition supports. A server with multiple databases runs the same
/// specifications in <see cref="Neo4jDatabaseMode.PerNamespace"/> mode: set <c language="text">CHRONICLE_NEO4J_IMAGE</c> to an Enterprise image and
/// <c language="text">CHRONICLE_NEO4J_DATABASE_MODE</c> to <c language="text">PerNamespace</c>. The graph projection specifications of the Client suite
/// read the same two variables.
/// </para>
/// </remarks>
public class Neo4jFixture : IAsyncLifetime
{
    /// <summary>
    /// The name of the environment variable that names the database mode.
    /// </summary>
    public const string ModeVariable = "CHRONICLE_NEO4J_DATABASE_MODE";

    Neo4jGraphServer? _server;

    /// <summary>
    /// Gets the Neo4j database mode the sink runs in.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the environment variable names something that is not a database mode.</exception>
    public Neo4jDatabaseMode DatabaseMode { get; } = Enum.Parse<Neo4jDatabaseMode>(Environment.GetEnvironmentVariable(ModeVariable) is { Length: > 0 } mode ? mode : nameof(Neo4jDatabaseMode.Single), true);

    /// <summary>
    /// Gets a value indicating whether every namespace shares one database.
    /// </summary>
    public bool IsSingleDatabase => DatabaseMode == Neo4jDatabaseMode.Single;

    /// <summary>
    /// Gets the Neo4j server.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the server has not started.</exception>
    public Neo4jGraphServer Server => _server ?? throw new InvalidOperationException("The Neo4j server has not started.");

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown when the database mode needs multiple databases and the server has one.</exception>
    public async ValueTask InitializeAsync()
    {
        _server = await Task.Run(() => Neo4jGraphServer.Start());
        if (!IsSingleDatabase && !_server.IsEnterprise)
        {
            throw new InvalidOperationException(
                $"{ModeVariable} is {DatabaseMode}, which needs a server with multiple databases, but {_server.Image} has one. Set {Neo4jGraphServer.ImageVariable} to an Enterprise image.");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_server is not null)
        {
            await _server.DisposeAsync();
        }
    }
}
