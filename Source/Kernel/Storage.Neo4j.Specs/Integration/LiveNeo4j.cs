// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Neo4j.Integration;

/// <summary>
/// Describes the live Neo4j server the integration specs run against.
/// </summary>
/// <remarks>
/// The specs are opt-in: they run only when <see cref="UriVariable"/> is set, so a build without a Neo4j server skips them.
/// They use single database mode, which works on a Neo4j edition with one database, and give every run its own namespaces.
/// </remarks>
public static class LiveNeo4j
{
    /// <summary>
    /// The environment variable holding the bolt URI of the server, for instance <c language="text">bolt://localhost:7687</c>.
    /// </summary>
    public const string UriVariable = "CHRONICLE_NEO4J_URI";

    /// <summary>
    /// The environment variable holding the user name. Defaults to <c language="text">neo4j</c>.
    /// </summary>
    public const string UserVariable = "CHRONICLE_NEO4J_USER";

    /// <summary>
    /// The environment variable holding the password. It is only ever passed on to the driver.
    /// </summary>
    public const string PasswordVariable = "CHRONICLE_NEO4J_PASSWORD";

    /// <summary>
    /// The prefix of the namespaces the specs create, which are removed again.
    /// </summary>
    public const string NamespacePrefix = "specs-";

    /// <summary>
    /// Gets a value indicating whether a live server is configured.
    /// </summary>
    public static bool Enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(UriVariable));

    /// <summary>
    /// Gets the reason the live specs are skipped.
    /// </summary>
    public static string SkipReason => $"Live Neo4j specs are opt-in. Set {UriVariable} (and {UserVariable} and {PasswordVariable}) to run them against a Neo4j server.";

    /// <summary>
    /// Create the connections the sink uses, in single database mode.
    /// </summary>
    /// <param name="driverFactory">The <see cref="Neo4jDriverFactory"/> to share drivers through.</param>
    /// <returns>The <see cref="Neo4jConnections"/>.</returns>
    public static Neo4jConnections CreateConnections(Neo4jDriverFactory driverFactory)
    {
        var options = Options.Create(new ChronicleOptions
        {
            Neo4j = new()
            {
                Server = Environment.GetEnvironmentVariable(UriVariable)!,
                UserName = Environment.GetEnvironmentVariable(UserVariable) ?? "neo4j",
                PasswordSecret = PasswordVariable,
                DatabaseMode = Neo4jDatabaseMode.Single
            }
        });
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var factory = new FixedResolverFactory();
        return new(options, configuration, driverFactory, new StorageConnectionResolution(Neo4jConnectionStorage.Name, new(), factory));
    }

    /// <summary>
    /// Remove every node a spec created in its namespaces.
    /// </summary>
    /// <param name="driverFactory">The <see cref="Neo4jDriverFactory"/> sharing the drivers.</param>
    /// <param name="namespaces">The namespaces the spec used. Other specs run in parallel on the same database, so only these are removed.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task Clean(Neo4jDriverFactory driverFactory, IEnumerable<string> namespaces)
    {
        var connection = await CreateConnections(driverFactory).Resolve("specs", "specs", ConnectionPurpose.Runtime);
        await using var session = connection.OpenSession();
        var cursor = await session.RunAsync(
            "MATCH (n) WHERE n.`_namespace` IN $namespaces DETACH DELETE n",
            new Dictionary<string, object> { ["namespaces"] = namespaces.ToList() });
        await cursor.ConsumeAsync();
    }

    class FixedResolverFactory : IConnectionResolverFactory
    {
        public ICachingConnectionResolver Create(ConnectionResolutionOptions options) => throw new NotSupportedException("Resolution is not configured for the live specs.");
    }
}
