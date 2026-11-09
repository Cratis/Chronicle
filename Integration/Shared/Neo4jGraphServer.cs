// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Neo4j.Driver;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents the Neo4j server the graph specifications run against: the graph projection specifications of the client suite and the
/// storage level specifications of the Neo4j suite share it.
/// </summary>
/// <remarks>
/// The server is the Community edition unless <c language="text">CHRONICLE_NEO4J_IMAGE</c> names another, and that has one user database, so the sink
/// runs in single database mode. The password is generated for every run and only ever leaves this type as an environment variable of
/// the containers and as a configuration value.
/// </remarks>
public sealed class Neo4jGraphServer : IAsyncDisposable
{
    /// <summary>
    /// The image of the server unless <c language="text">CHRONICLE_NEO4J_IMAGE</c> names another. The Community edition needs no license agreement.
    /// </summary>
    public const string DefaultImage = "neo4j:5-community";

    /// <summary>
    /// The name of the environment variable that names the image of the server.
    /// </summary>
    public const string ImageVariable = "CHRONICLE_NEO4J_IMAGE";

    /// <summary>
    /// The host name of the server on the docker network, which is how the kernel container reaches it.
    /// </summary>
    public const string HostName = "chronicle-neo4j";

    /// <summary>
    /// The user the server is set up with.
    /// </summary>
    public const string UserName = "neo4j";

    /// <summary>
    /// The name of the only user database of the Community edition.
    /// </summary>
    public const string Database = "neo4j";

    const int BoltPort = 7687;

    readonly IContainer _container;
    IDriver? _driver;

    Neo4jGraphServer(IContainer container, string password, string image)
    {
        _container = container;
        Password = password;
        Image = image;
    }

    /// <summary>
    /// Gets the image the server runs.
    /// </summary>
    public string Image { get; }

    /// <summary>
    /// Gets a value indicating whether the server is the Enterprise edition, the only one with multiple databases.
    /// </summary>
    public bool IsEnterprise => IsEnterpriseImage(Image);

    /// <summary>
    /// Gets the generated password.
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// Gets the bolt URI the kernel container reaches the server on, through the docker network.
    /// </summary>
    public string NetworkUri => $"bolt://{HostName}:{BoltPort}";

    /// <summary>
    /// Gets the bolt URI of the server as seen from the test process.
    /// </summary>
    public string HostUri => $"bolt://localhost:{_container.GetMappedPublicPort(BoltPort)}";

    /// <summary>
    /// Gets the driver the specifications read the graph with, which is separate from the sink under test.
    /// </summary>
    public IDriver Driver => _driver ??= GraphDatabase.Driver(HostUri, AuthTokens.Basic(UserName, Password));

    /// <summary>
    /// Start a server, optionally on a network.
    /// </summary>
    /// <param name="network">The <see cref="INetwork"/> to attach the server to, which is what lets a kernel container reach it. Without one, only the test process reaches the server.</param>
    /// <returns>The started <see cref="Neo4jGraphServer"/>.</returns>
    public static Neo4jGraphServer Start(INetwork? network = null)
    {
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var image = Environment.GetEnvironmentVariable(ImageVariable) is { Length: > 0 } named ? named : DefaultImage;

        // The readiness probe reads the credentials from the environment of the container, so the generated password is in no command line Testcontainers logs.
        var builder = new ContainerBuilder(image)
            .WithImage(image)
            .WithPortBinding(BoltPort, assignRandomHostPort: true)
            .WithEnvironment("NEO4J_AUTH", $"{UserName}/{password}")
            .WithEnvironment("NEO4J_server_memory_heap_max__size", "512M")
            .WithEnvironment("NEO4J_server_memory_pagecache_size", "128M")
            .WithTmpfsMount("/data")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(BoltPort)
                .UntilCommandIsCompleted("/bin/sh", "-c", "cypher-shell -u \"${NEO4J_AUTH%%/*}\" -p \"${NEO4J_AUTH#*/}\" 'RETURN 1'"));

        // The Enterprise edition, the only one with multiple databases, will not start without a license decision.
        // The Evaluation Agreement is what the per namespace lane runs under for now; the license follow-up is tracked in Cratis/Chronicle#5739.
        if (IsEnterpriseImage(image))
        {
            builder = builder.WithEnvironment("NEO4J_ACCEPT_LICENSE_AGREEMENT", "eval");
        }

        if (network is not null)
        {
            builder = builder.WithHostname(HostName).WithNetwork(network);
        }

        var container = builder.Build();
        container.StartAsync().GetAwaiter().GetResult();
        return new(container, password, image);
    }

    /// <summary>
    /// Remove everything the specifications wrote, so that every specification starts on an empty graph. Constraints, indexes and databases stay.
    /// </summary>
    /// <param name="allDatabases">Whether to clear every user database, which is what a server with a database per namespace needs.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Clear(bool allDatabases = false)
    {
        foreach (var database in allDatabases ? await UserDatabases() : [Database])
        {
            await using var session = Driver.AsyncSession(_ => _.WithDatabase(database));
            var cursor = await session.RunAsync("MATCH (n) DETACH DELETE n");
            await cursor.ConsumeAsync();
        }
    }

    static bool IsEnterpriseImage(string image) => image.Contains("enterprise", StringComparison.OrdinalIgnoreCase);

    async Task<List<string>> UserDatabases()
    {
        await using var session = Driver.AsyncSession(_ => _.WithDatabase("system"));
        var cursor = await session.RunAsync("SHOW DATABASES YIELD name, currentStatus WHERE name <> \"system\" AND currentStatus = \"online\" RETURN name");
        return [.. (await cursor.ToListAsync()).Select(_ => _["name"].As<string>())];
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_driver is not null)
        {
            await _driver.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
