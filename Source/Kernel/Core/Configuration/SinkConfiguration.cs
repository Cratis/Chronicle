// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents one named sink: a sink type and the connection it stores to, independent of the kernel's event storage.
/// </summary>
public class SinkConfiguration
{
    /// <summary>
    /// Gets the sink type: <c language="text">MongoDB</c> or <c language="text">SQL</c> for read models, <c language="text">Neo4j</c> for graphs.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the connection string of a <c language="text">MongoDB</c> or <c language="text">SQL</c> sink. The SQL engine (PostgreSQL, Microsoft SQL Server or SQLite) is read from it.
    /// </summary>
    public string ConnectionDetails { get; init; } = string.Empty;

    /// <summary>
    /// Gets the path of a file holding the <see cref="ConnectionDetails"/>, for instance a mounted Kubernetes Secret.
    /// </summary>
    /// <remarks>
    /// The file is read once at startup and trimmed of a trailing newline. Setting both this and <see cref="ConnectionDetails"/>
    /// stops startup. See <see cref="ConnectionFiles"/>.
    /// </remarks>
    public string ConnectionDetailsFile { get; init; } = string.Empty;

    /// <summary>
    /// Gets the server URI of a <c language="text">Neo4j</c> sink, for instance <c language="text">bolt://neo4j:7687</c>.
    /// </summary>
    public string Server { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional user name a <c language="text">Neo4j</c> sink authenticates with.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets the name of the configuration key holding the password of a <c language="text">Neo4j</c> sink. The password itself is never written in the options.
    /// </summary>
    public string? PasswordSecret { get; init; }

    /// <summary>
    /// Gets the path of a file holding the password of a <c language="text">Neo4j</c> sink, for instance a mounted Kubernetes Secret.
    /// </summary>
    /// <remarks>
    /// The file is read once at startup and trimmed of a trailing newline, and <see cref="PasswordSecret"/> is pointed at the value.
    /// Setting both this and <see cref="PasswordSecret"/> stops startup. See <see cref="ConnectionFiles"/>.
    /// </remarks>
    public string? PasswordFile { get; init; }

    /// <summary>
    /// Gets the optional prefix of the database names of a <c language="text">Neo4j</c> sink.
    /// </summary>
    public string? DatabasePrefix { get; init; }

    /// <summary>
    /// Gets how a <c language="text">Neo4j</c> sink places the graphs of the event stores and namespaces. Defaults to <see cref="Neo4jDatabaseMode.PerNamespace"/>.
    /// </summary>
    public Neo4jDatabaseMode DatabaseMode { get; init; } = Neo4jDatabaseMode.PerNamespace;

    /// <summary>
    /// Gets the name of the database a <c language="text">Neo4j</c> sink uses in <see cref="Neo4jDatabaseMode.Single"/> mode.
    /// </summary>
    public string SingleDatabase { get; init; } = Neo4jOptions.DefaultSingleDatabase;

    /// <summary>
    /// Gets a value indicating whether a missing database is created on first use. Defaults to true.
    /// </summary>
    public bool CreateIfMissing { get; init; } = true;

    /// <summary>
    /// Get the <see cref="Neo4jOptions"/> of a <c language="text">Neo4j</c> sink.
    /// </summary>
    /// <returns>The <see cref="Neo4jOptions"/>.</returns>
    public Neo4jOptions ToNeo4jOptions() => new()
    {
        Server = Server,
        UserName = UserName,
        PasswordSecret = PasswordSecret,
        DatabasePrefix = DatabasePrefix,
        DatabaseMode = DatabaseMode,
        SingleDatabase = SingleDatabase,
        CreateIfMissing = CreateIfMissing
    };
}
