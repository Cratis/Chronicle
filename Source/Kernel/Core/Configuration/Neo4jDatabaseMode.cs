// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines how the Neo4j graph sink places the graphs of the event stores and namespaces.
/// </summary>
public enum Neo4jDatabaseMode
{
    /// <summary>
    /// One Neo4j database per event store and namespace. This is the mode for production.
    /// </summary>
    PerNamespace = 0,

    /// <summary>
    /// One Neo4j database for everything, with the namespace stamped as <c language="text">_namespace</c> on every node and relationship.
    /// </summary>
    /// <remarks>
    /// Meant for development and test, where a Neo4j edition without multiple databases is common. Nothing isolates a
    /// namespace from the others except the filter a query applies.
    /// </remarks>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Single is the name of the mode in the configuration; it describes one database, not a type.")]
    Single = 1
}
