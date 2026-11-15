// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Defines the connections to the Neo4j databases of the graphs.
/// </summary>
public interface INeo4jConnections
{
    /// <summary>
    /// Gets the <see cref="Neo4jOptions"/>.
    /// </summary>
    Neo4jOptions Options { get; }

    /// <summary>
    /// Resolve the connection to the database of a graph.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> of the graph.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> of the graph.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>: <see cref="ConnectionPurpose.Runtime"/> for reading and writing, <see cref="ConnectionPurpose.Schema"/> for creating the database and its constraints.</param>
    /// <returns>The <see cref="Neo4jConnection"/>.</returns>
    ValueTask<Neo4jConnection> Resolve(EventStoreName eventStore, EventStoreNamespaceName @namespace, ConnectionPurpose purpose);
}
