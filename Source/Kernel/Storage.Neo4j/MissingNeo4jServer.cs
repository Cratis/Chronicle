// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Exception that gets thrown when a graph is used and no Neo4j server is configured or resolved for it.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> of the graph.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> of the graph.</param>
public class MissingNeo4jServer(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    : Exception($"No Neo4j server is configured for the graph of event store '{eventStore}' and namespace '{@namespace}'. Set 'Neo4j:Server' or resolve a connection string for the storage '{Neo4jConnectionStorage.Name}'.");
