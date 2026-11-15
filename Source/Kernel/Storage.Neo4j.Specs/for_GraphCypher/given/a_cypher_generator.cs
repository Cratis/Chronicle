// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphCypher.given;

public class a_cypher_generator : Specification
{
    protected GraphCypher _cypher;

    protected static IReadOnlyDictionary<string, object?> NoProperties => new Dictionary<string, object?>();

    protected static NodeReference Person(object key) => new("Person", key);

    protected static NodeReference Group(object key) => new("Group", key);

    protected static IReadOnlyDictionary<string, object?> Props(params (string Name, object? Value)[] properties) =>
        properties.ToDictionary(_ => _.Name, _ => _.Value);

    void Establish() => _cypher = new(GraphDefinitions.Organization, null);
}
