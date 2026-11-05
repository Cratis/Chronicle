// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for the Neo4j graph sink.
/// </summary>
public class Neo4jOptions
{
    /// <summary>
    /// The default name of the database in <see cref="Neo4jDatabaseMode.Single"/> mode.
    /// </summary>
    public const string DefaultSingleDatabase = "neo4j";

    /// <summary>
    /// Gets the server URI, for instance <c language="text">bolt://localhost:7687</c> or <c language="text">neo4j+s://graph.example</c>.
    /// </summary>
    /// <remarks>
    /// Leave it empty when the connection resolution gives a bolt URI for every graph.
    /// </remarks>
    public string Server { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional user name to authenticate with.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets the name of the configuration key holding the password, for instance <c language="text">Neo4j:Password</c> or an environment
    /// variable name such as <c language="text">NEO4J_PASSWORD</c>.
    /// </summary>
    /// <remarks>
    /// The key is read from the configuration when the connection is made, so the password can come from any configuration
    /// provider and is never written in the options.
    /// </remarks>
    public string? PasswordSecret { get; init; }

    /// <summary>
    /// Gets the optional prefix of the database names, for sharing one Neo4j server between applications.
    /// </summary>
    public string? DatabasePrefix { get; init; }

    /// <summary>
    /// Gets how the graphs of the event stores and namespaces are placed. Defaults to <see cref="Neo4jDatabaseMode.PerNamespace"/>.
    /// </summary>
    public Neo4jDatabaseMode DatabaseMode { get; init; } = Neo4jDatabaseMode.PerNamespace;

    /// <summary>
    /// Gets the name of the database used in <see cref="Neo4jDatabaseMode.Single"/> mode. Defaults to <see cref="DefaultSingleDatabase"/>.
    /// </summary>
    public string SingleDatabase { get; init; } = DefaultSingleDatabase;

    /// <summary>
    /// Gets a value indicating whether a missing database is created on first use. Defaults to true.
    /// </summary>
    /// <remarks>
    /// Turn it off when the identity is not allowed to create databases, or a platform provisions them. A missing database
    /// then fails with the error of the server. It has no effect in <see cref="Neo4jDatabaseMode.Single"/> mode.
    /// </remarks>
    public bool CreateIfMissing { get; init; } = true;
}
