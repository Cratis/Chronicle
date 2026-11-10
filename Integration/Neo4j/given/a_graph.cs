// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Chronicle.Neo4j.Integration.given;

/// <summary>
/// A graph on the real server, read back with Cypher of the specification's own, through a driver that is not the one the sink under test uses.
/// </summary>
/// <param name="fixture">The <see cref="Neo4jFixture"/> providing the Neo4j server.</param>
public class a_graph(Neo4jFixture fixture) : Neo4jSpecification(fixture)
{
    /// <summary>
    /// Read the properties of a person straight from the database, bookkeeping included.
    /// </summary>
    /// <param name="key">The key of the person.</param>
    /// <param name="namespace">The namespace to read in. Defaults to the namespace of the specification.</param>
    /// <returns>The properties, or null when there is no such person.</returns>
    protected Task<IReadOnlyDictionary<string, object>?> StoredPerson(string key, string? @namespace = null) => StoredNode("Person", "personId", key, @namespace);

    /// <summary>
    /// Read the properties of a group straight from the database, bookkeeping included.
    /// </summary>
    /// <param name="key">The key of the group.</param>
    /// <param name="namespace">The namespace to read in. Defaults to the namespace of the specification.</param>
    /// <returns>The properties, or null when there is no such group.</returns>
    protected Task<IReadOnlyDictionary<string, object>?> StoredGroup(string key, string? @namespace = null) => StoredNode("Group", "groupId", key, @namespace);

    /// <summary>
    /// Read the properties of a node straight from the database, bookkeeping included.
    /// </summary>
    /// <param name="label">The label of the node.</param>
    /// <param name="keyProperty">The name of the key property.</param>
    /// <param name="key">The key of the node.</param>
    /// <param name="namespace">The namespace to read in. Defaults to the namespace of the specification.</param>
    /// <returns>The properties, or null when there is no such node.</returns>
    protected async Task<IReadOnlyDictionary<string, object>?> StoredNode(string label, string keyProperty, string key, string? @namespace = null)
    {
        var records = await Read(
            $"MATCH (n:`{label}` {{`{keyProperty}`: $key}}){NamespaceFilter("n")} RETURN properties(n) AS properties",
            Parameters(@namespace, ("key", key)),
            @namespace);
        return records.Count == 0 ? null : records[0]["properties"].As<IReadOnlyDictionary<string, object>>();
    }

    /// <summary>
    /// Read the edges of a type straight from the database, bookkeeping included, ordered by their ends and their properties.
    /// </summary>
    /// <param name="type">The type of the edges.</param>
    /// <param name="namespace">The namespace to read in. Defaults to the namespace of the specification.</param>
    /// <returns>The properties of every edge.</returns>
    protected async Task<IReadOnlyList<IReadOnlyDictionary<string, object>>> StoredEdges(string type, string? @namespace = null)
    {
        var records = await Read(
            $"MATCH (s)-[r:`{type}`]->(t){NamespaceFilter("s")} RETURN properties(r) AS properties, s.personId AS source, t.groupId AS target ORDER BY source, target, toString(r.visitId)",
            Parameters(@namespace),
            @namespace);
        return [.. records.Select(_ => (IReadOnlyDictionary<string, object>)new Dictionary<string, object>(_["properties"].As<IReadOnlyDictionary<string, object>>())
        {
            ["source"] = _["source"],
            ["target"] = _["target"]
        })];
    }

    /// <summary>
    /// Count the nodes with a label straight in the database.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="namespace">The namespace to count in. Defaults to the namespace of the specification.</param>
    /// <returns>The number of nodes.</returns>
    protected async Task<long> CountNodes(string label, string? @namespace = null) =>
        (await Read($"MATCH (n:`{label}`){NamespaceFilter("n")} RETURN count(n) AS total", Parameters(@namespace), @namespace))[0]["total"].As<long>();

    /// <summary>
    /// Count the edges of a type straight in the database.
    /// </summary>
    /// <param name="type">The type of the edges.</param>
    /// <param name="namespace">The namespace to count in. Defaults to the namespace of the specification.</param>
    /// <returns>The number of edges.</returns>
    protected async Task<long> CountEdges(string type, string? @namespace = null) =>
        (await Read($"MATCH (s)-[r:`{type}`]->(){NamespaceFilter("s")} RETURN count(r) AS total", Parameters(@namespace), @namespace))[0]["total"].As<long>();

    /// <summary>
    /// Get the filter that keeps a query to the namespace, which only a database that several namespaces share needs.
    /// </summary>
    /// <param name="alias">The alias of the node the filter is on.</param>
    /// <returns>The text to add to a <c language="text">MATCH</c>.</returns>
    protected string NamespaceFilter(string alias) => IsSingleDatabase ? $" WHERE {alias}.`_namespace` = $namespace" : string.Empty;

    /// <summary>
    /// Create the parameters of a statement, with the namespace.
    /// </summary>
    /// <param name="namespace">The namespace. Defaults to the namespace of the specification.</param>
    /// <param name="parameters">More parameters.</param>
    /// <returns>The parameters.</returns>
    protected Dictionary<string, object> Parameters(string? @namespace, params (string Name, object Value)[] parameters)
    {
        var result = new Dictionary<string, object> { ["namespace"] = @namespace ?? Namespace };
        foreach (var (name, value) in parameters) result[name] = value;
        return result;
    }
}
