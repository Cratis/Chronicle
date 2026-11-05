// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Provides the graph definition the specs run against: Person and Group nodes, a MEMBER_OF edge and a VISITED edge
/// that is identified by a property. It is the graph of the in-memory graph sink specs.
/// </summary>
public static class GraphDefinitions
{
    /// <summary>
    /// Gets the organization graph.
    /// </summary>
    public static GraphDefinition Organization => new(
        "Organization",
        [
            new("Person", "personId", Schema(("personId", "string"), ("name", "string"), ("visits", "integer"))),
            new("Group", "groupId", Schema(("groupId", "string"), ("name", "string")))
        ],
        [
            new("MEMBER_OF", "Person", "Group", null, Schema(("since", "string"))),
            new("VISITED", "Person", "Group", "visitId", Schema(("visitId", "string"), ("count", "integer")))
        ]);

    /// <summary>
    /// Create a schema.
    /// </summary>
    /// <param name="properties">The properties by name and type.</param>
    /// <returns>The <see cref="JsonSchema"/>.</returns>
    public static JsonSchema Schema(params (string Name, string Type)[] properties)
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        foreach (var (name, type) in properties)
        {
            schema.Properties[name] = new JsonSchemaProperty { Type = type == "integer" ? JsonObjectType.Integer : JsonObjectType.String };
        }

        return schema;
    }
}
