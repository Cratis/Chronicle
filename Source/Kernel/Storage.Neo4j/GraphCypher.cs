// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Generates the Cypher statements of a graph.
/// </summary>
/// <remarks>
/// <para>
/// Every label, relationship type and property name in a statement comes from the <see cref="GraphDefinition"/>, looked up
/// by the label or type of the element, and is escaped with backticks. A label or type that the definition does not know
/// throws <see cref="UnknownGraphElement"/> instead of reaching the text. Keys, identities, property values, the owner and
/// the sequence number are parameters, never part of the text.
/// </para>
/// <para>
/// In single database mode, <paramref name="namespace"/> is set: every node and relationship carries it as
/// <see cref="GraphBookkeeping.Namespace"/>, and every node is identified by its key and its namespace.
/// </para>
/// </remarks>
/// <param name="definition">The <see cref="GraphDefinition"/> of the graph.</param>
/// <param name="namespace">The namespace to stamp on everything in single database mode, or null in per-namespace mode.</param>
public class GraphCypher(GraphDefinition definition, string? @namespace)
{
    /// <summary>
    /// Gets whether the graph shares its database with other namespaces.
    /// </summary>
    public bool IsSingleDatabase => @namespace is not null;

    /// <summary>
    /// Escape an identifier so it can be part of Cypher text.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The identifier in backticks, with backticks inside doubled.</returns>
    public static string Escape(string identifier) => $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`";

    /// <summary>
    /// Create the statement that creates the uniqueness constraint of a node label.
    /// </summary>
    /// <param name="node">The <see cref="GraphNodeDefinition"/>.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement CreateNodeConstraint(GraphNodeDefinition node)
    {
        var properties = IsSingleDatabase
            ? $"(n.{Escape(node.KeyProperty)}, n.{Escape(GraphBookkeeping.Namespace)})"
            : $"n.{Escape(node.KeyProperty)}";
        var name = ConstraintName($"node_{node.Label}_{node.KeyProperty}");
        return Statement($"CREATE CONSTRAINT {Escape(name)} IF NOT EXISTS FOR (n:{Escape(node.Label)}) REQUIRE {properties} IS UNIQUE");
    }

    /// <summary>
    /// Create the statement that creates the uniqueness constraint of the nodes that keep the replay generation of an owner.
    /// </summary>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement CreateOwnerConstraint()
    {
        var properties = IsSingleDatabase
            ? $"(o.{Escape(GraphBookkeeping.Owner)}, o.{Escape(GraphBookkeeping.Namespace)})"
            : $"o.{Escape(GraphBookkeeping.Owner)}";
        return Statement($"CREATE CONSTRAINT {Escape(ConstraintName("owner"))} IF NOT EXISTS FOR (o:{Escape(GraphBookkeeping.OwnerLabel)}) REQUIRE {properties} IS UNIQUE");
    }

    /// <summary>
    /// Create the statement that creates the index on the identifying property of an edge type.
    /// </summary>
    /// <param name="edge">The <see cref="GraphEdgeDefinition"/>, which must have an identifying property.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement CreateEdgeIndex(GraphEdgeDefinition edge)
    {
        var name = ConstraintName($"edge_{edge.Type}_{edge.IdentifyingProperty}");
        return Statement($"CREATE INDEX {Escape(name)} IF NOT EXISTS FOR ()-[r:{Escape(edge.Type)}]-() ON (r.{Escape(edge.IdentifyingProperty!)})");
    }

    /// <summary>
    /// Create the statement that reads a node: its properties, bookkeeping included, and the number of edges attached to it.
    /// </summary>
    /// <param name="node">The <see cref="NodeReference"/>.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement ReadNode(NodeReference node)
    {
        var parameters = NodeParameters(node, "key");
        return new($"MATCH {NodePattern("n", node, "key")} RETURN properties(n) AS properties, COUNT {{ (n)--() }} AS edges", parameters);
    }

    /// <summary>
    /// Create the statement that reads an edge: its properties, bookkeeping included.
    /// </summary>
    /// <param name="edge">The <see cref="EdgeReference"/>.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement ReadEdge(EdgeReference edge)
    {
        var (match, parameters) = EdgeMatch(edge);
        return new($"{match} RETURN properties(r) AS properties LIMIT 1", parameters);
    }

    /// <summary>
    /// Create the statement that makes a node bare: all its properties are replaced by its key.
    /// </summary>
    /// <param name="node">The <see cref="NodeReference"/>.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement MakeNodeBare(NodeReference node)
    {
        var parameters = NodeParameters(node, "key");
        parameters["base"] = BaseProperties(node);
        return new($"MATCH {NodePattern("n", node, "key")} SET n = $base", parameters);
    }

    /// <summary>
    /// Create the statement that creates a node when it does not exist, sets properties on it and stamps it.
    /// </summary>
    /// <param name="node">The <see cref="NodeReference"/>.</param>
    /// <param name="properties">The properties to set. Null removes a property.</param>
    /// <param name="owner">The owner to stamp when the node is unowned or already owned by it.</param>
    /// <param name="sequence">The event sequence number, which becomes the watermark.</param>
    /// <param name="generation">The replay generation of the owner.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement WriteNode(NodeReference node, IReadOnlyDictionary<string, object?> properties, string owner, long sequence, long generation)
    {
        var parameters = NodeParameters(node, "key");
        AddWrite(parameters, properties.Where(_ => _.Key != definition.GetNode(node.Label).KeyProperty).ToDictionary(_ => _.Key, _ => _.Value), owner, sequence, generation);
        return new($"MERGE {NodePattern("n", node, "key")} {WriteClause("n")}", parameters);
    }

    /// <summary>
    /// Create the statement that sets properties on a node that exists, and stamps it.
    /// </summary>
    /// <param name="node">The <see cref="NodeReference"/>.</param>
    /// <param name="properties">The properties to set. Null removes a property.</param>
    /// <param name="owner">The owner to stamp when the node is unowned or already owned by it.</param>
    /// <param name="sequence">The event sequence number, which becomes the watermark.</param>
    /// <param name="generation">The replay generation of the owner.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement SetNodeProperties(NodeReference node, IReadOnlyDictionary<string, object?> properties, string owner, long sequence, long generation)
    {
        var parameters = NodeParameters(node, "key");
        AddWrite(parameters, properties.Where(_ => _.Key != definition.GetNode(node.Label).KeyProperty).ToDictionary(_ => _.Key, _ => _.Value), owner, sequence, generation);
        return new($"MATCH {NodePattern("n", node, "key")} {WriteClause("n")}", parameters);
    }

    /// <summary>
    /// Create the statement that deletes a node.
    /// </summary>
    /// <param name="node">The <see cref="NodeReference"/>.</param>
    /// <param name="detach">Whether the edges attached to the node are deleted with it.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement DeleteNode(NodeReference node, bool detach) =>
        new($"MATCH {NodePattern("n", node, "key")} {(detach ? "DETACH DELETE" : "DELETE")} n", NodeParameters(node, "key"));

    /// <summary>
    /// Create the statement that makes sure a node exists as an end of an edge: a node that does not exist is created bare.
    /// </summary>
    /// <param name="node">The <see cref="NodeReference"/>.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement EnsureEndpoint(NodeReference node) =>
        new($"MERGE {NodePattern("n", node, "key")}", NodeParameters(node, "key"));

    /// <summary>
    /// Create the statement that creates an edge when it does not exist, sets properties on it and stamps it.
    /// </summary>
    /// <param name="edge">The <see cref="EdgeReference"/>. Both end nodes must exist.</param>
    /// <param name="properties">The properties to set. Null removes a property.</param>
    /// <param name="owner">The owner to stamp when the edge is unowned or already owned by it.</param>
    /// <param name="sequence">The event sequence number, which becomes the watermark.</param>
    /// <param name="generation">The replay generation of the owner.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement WriteEdge(EdgeReference edge, IReadOnlyDictionary<string, object?> properties, string owner, long sequence, long generation)
    {
        var definitionOfEdge = definition.GetEdge(edge.Type);
        var parameters = EndpointParameters(edge);
        if (edge.Identity is not null) parameters["identity"] = Neo4jValues.ToDriver(edge.Identity);
        AddWrite(parameters, properties.Where(_ => _.Key != definitionOfEdge.IdentifyingProperty).ToDictionary(_ => _.Key, _ => _.Value), owner, sequence, generation);
        var relationship = edge.Identity is null
            ? $"[r:{Escape(edge.Type)}]"
            : $"[r:{Escape(edge.Type)} {{{Escape(definitionOfEdge.IdentifyingProperty!)}: $identity}}]";
        var stamp = IsSingleDatabase ? $", r.{Escape(GraphBookkeeping.Namespace)} = $namespace" : string.Empty;
        return new(
            $"MATCH {NodePattern("s", edge.Source, "sourceKey")} MATCH {NodePattern("t", edge.Target, "targetKey")} " +
            $"MERGE (s)-{relationship}->(t) {WriteClause("r", stamp)}",
            parameters);
    }

    /// <summary>
    /// Create the statement that sets properties on an edge that exists, and stamps it.
    /// </summary>
    /// <param name="edge">The <see cref="EdgeReference"/>.</param>
    /// <param name="properties">The properties to set. Null removes a property.</param>
    /// <param name="owner">The owner to stamp when the edge is unowned or already owned by it.</param>
    /// <param name="sequence">The event sequence number, which becomes the watermark.</param>
    /// <param name="generation">The replay generation of the owner.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement SetEdgeProperties(EdgeReference edge, IReadOnlyDictionary<string, object?> properties, string owner, long sequence, long generation)
    {
        var definitionOfEdge = definition.GetEdge(edge.Type);
        var (match, parameters) = EdgeMatch(edge);
        AddWrite(parameters, properties.Where(_ => _.Key != definitionOfEdge.IdentifyingProperty).ToDictionary(_ => _.Key, _ => _.Value), owner, sequence, generation);
        return new($"{match} WITH r LIMIT 1 {WriteClause("r")}", parameters);
    }

    /// <summary>
    /// Create the statement that deletes an edge. The end nodes stay.
    /// </summary>
    /// <param name="edge">The <see cref="EdgeReference"/>.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement DeleteEdge(EdgeReference edge)
    {
        var (match, parameters) = EdgeMatch(edge);
        return new($"{match} DELETE r", parameters);
    }

    /// <summary>
    /// Create the statement that reads the current replay generation of an owner.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement ReadGeneration(string owner) =>
        new($"MATCH (o:{Escape(GraphBookkeeping.OwnerLabel)} {OwnerMap()}) RETURN o.{Escape(GraphBookkeeping.Generation)} AS generation", OwnerParameters(owner));

    /// <summary>
    /// Create the statement that records the current replay generation of an owner.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="generation">The generation.</param>
    /// <returns>The <see cref="CypherStatement"/>.</returns>
    public CypherStatement WriteGeneration(string owner, long generation)
    {
        var parameters = OwnerParameters(owner);
        parameters["generation"] = generation;
        return new($"MERGE (o:{Escape(GraphBookkeeping.OwnerLabel)} {OwnerMap()}) SET o.{Escape(GraphBookkeeping.Generation)} = $generation", parameters);
    }

    /// <summary>
    /// Create the statements that delete every edge of an owner that was not written in a replay generation.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="generation">The replay generation.</param>
    /// <returns>The <see cref="CypherStatement">statements</see>, one per edge type.</returns>
    public IEnumerable<CypherStatement> SweepEdges(string owner, long generation) =>
        definition.Edges.Select(edge => new CypherStatement(
            $"MATCH ()-[r:{Escape(edge.Type)}]->() WHERE {StaleCondition("r")}{NamespaceCondition("r")} DELETE r",
            SweepParameters(owner, generation)));

    /// <summary>
    /// Create the statements that turn every node of an owner that was not written in a replay generation, and that edges
    /// still point at, into a bare node.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="generation">The replay generation.</param>
    /// <returns>The <see cref="CypherStatement">statements</see>, one per node label.</returns>
    public IEnumerable<CypherStatement> MakeStaleNodesWithEdgesBare(string owner, long generation) =>
        definition.Nodes.Select(node =>
        {
            var keep = IsSingleDatabase
                ? $"{{{Escape(node.KeyProperty)}: n.{Escape(node.KeyProperty)}, {Escape(GraphBookkeeping.Namespace)}: n.{Escape(GraphBookkeeping.Namespace)}}}"
                : $"{{{Escape(node.KeyProperty)}: n.{Escape(node.KeyProperty)}}}";
            return new CypherStatement(
                $"MATCH (n:{Escape(node.Label)}) WHERE {StaleCondition("n")}{NamespaceCondition("n")} AND COUNT {{ (n)--() }} > 0 SET n = {keep}",
                SweepParameters(owner, generation));
        });

    /// <summary>
    /// Create the statements that delete every node of an owner that was not written in a replay generation and has no edges.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="generation">The replay generation.</param>
    /// <returns>The <see cref="CypherStatement">statements</see>, one per node label.</returns>
    public IEnumerable<CypherStatement> SweepNodes(string owner, long generation) =>
        definition.Nodes.Select(node => new CypherStatement(
            $"MATCH (n:{Escape(node.Label)}) WHERE {StaleCondition("n")}{NamespaceCondition("n")} AND COUNT {{ (n)--() }} = 0 DELETE n",
            SweepParameters(owner, generation)));

    /// <summary>
    /// Create the statements that remove everything an owner wrote: its edges first, then its nodes with whatever is attached.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <returns>The <see cref="CypherStatement">statements</see>.</returns>
    public IEnumerable<CypherStatement> RemoveOwned(string owner)
    {
        var parameters = OwnerParameters(owner);
        foreach (var edge in definition.Edges)
        {
            yield return new($"MATCH ()-[r:{Escape(edge.Type)}]->() WHERE r.{Escape(GraphBookkeeping.Owner)} = $owner{NamespaceCondition("r")} DELETE r", parameters);
        }

        foreach (var node in definition.Nodes)
        {
            yield return new($"MATCH (n:{Escape(node.Label)}) WHERE n.{Escape(GraphBookkeeping.Owner)} = $owner{NamespaceCondition("n")} DETACH DELETE n", parameters);
        }
    }

    /// <summary>
    /// Create the statements that count and read a page of the nodes with a label.
    /// </summary>
    /// <param name="label">The node label.</param>
    /// <param name="skip">Number of nodes to skip.</param>
    /// <param name="take">Number of nodes to take.</param>
    /// <returns>The count statement and the page statement.</returns>
    public (CypherStatement Count, CypherStatement Page) ReadNodes(string label, int skip, int take)
    {
        var node = definition.GetNode(label);
        var where = IsSingleDatabase ? $" WHERE n.{Escape(GraphBookkeeping.Namespace)} = $namespace" : string.Empty;
        var parameters = PageParameters(skip, take);
        return (
            new($"MATCH (n:{Escape(node.Label)}){where} RETURN count(n) AS total", parameters),
            new($"MATCH (n:{Escape(node.Label)}){where} RETURN properties(n) AS properties ORDER BY toString(n.{Escape(node.KeyProperty)}) SKIP $skip LIMIT $take", parameters));
    }

    /// <summary>
    /// Create the statements that count and read a page of the edges of a type.
    /// </summary>
    /// <param name="type">The edge type.</param>
    /// <param name="skip">Number of edges to skip.</param>
    /// <param name="take">Number of edges to take.</param>
    /// <returns>The count statement and the page statement.</returns>
    public (CypherStatement Count, CypherStatement Page) ReadEdges(string type, int skip, int take)
    {
        var edge = definition.GetEdge(type);
        var source = definition.GetNode(edge.SourceLabel);
        var target = definition.GetNode(edge.TargetLabel);
        var pattern = $"MATCH (s:{Escape(source.Label)})-[r:{Escape(edge.Type)}]->(t:{Escape(target.Label)})";
        var where = IsSingleDatabase ? $" WHERE s.{Escape(GraphBookkeeping.Namespace)} = $namespace" : string.Empty;
        var identity = edge.IdentifyingProperty is null ? "''" : $"coalesce(toString(r.{Escape(edge.IdentifyingProperty)}), '')";
        var parameters = PageParameters(skip, take);
        return (
            new($"{pattern}{where} RETURN count(r) AS total", parameters),
            new(
                $"{pattern}{where} RETURN properties(r) AS properties, s.{Escape(source.KeyProperty)} AS source, t.{Escape(target.KeyProperty)} AS target " +
                $"ORDER BY toString(s.{Escape(source.KeyProperty)}), toString(t.{Escape(target.KeyProperty)}), {identity} SKIP $skip LIMIT $take",
                parameters));
    }

    static void AddWrite(Dictionary<string, object?> parameters, IReadOnlyDictionary<string, object?> properties, string owner, long sequence, long generation)
    {
        parameters["properties"] = Neo4jValues.ToDriver(properties);
        parameters["owner"] = owner;
        parameters["sequence"] = sequence;
        parameters["generation"] = generation;
    }

    static CypherStatement Statement(string text) => new(text, new Dictionary<string, object?>());

    static string ConstraintName(string name) => $"chronicle_{name}";

    static string StaleCondition(string alias) =>
        $"{alias}.{Escape(GraphBookkeeping.Owner)} = $owner AND {alias}.{Escape(GraphBookkeeping.Generation)} < $generation";

    static string WriteClause(string alias, string stamp = "") =>
        $"SET {alias} += $properties, {alias}.{Escape(GraphBookkeeping.Watermark)} = $sequence{stamp} " +
        $"WITH {alias} WHERE {alias}.{Escape(GraphBookkeeping.Owner)} IS NULL OR {alias}.{Escape(GraphBookkeeping.Owner)} = $owner " +
        $"SET {alias}.{Escape(GraphBookkeeping.Owner)} = $owner, {alias}.{Escape(GraphBookkeeping.Generation)} = $generation";

    string NamespaceCondition(string alias) => IsSingleDatabase ? $" AND {alias}.{Escape(GraphBookkeeping.Namespace)} = $namespace" : string.Empty;

    string OwnerMap() => IsSingleDatabase
        ? $"{{{Escape(GraphBookkeeping.Owner)}: $owner, {Escape(GraphBookkeeping.Namespace)}: $namespace}}"
        : $"{{{Escape(GraphBookkeeping.Owner)}: $owner}}";

    string NodePattern(string alias, NodeReference node, string keyParameter)
    {
        var definitionOfNode = definition.GetNode(node.Label);
        var stamp = IsSingleDatabase ? $", {Escape(GraphBookkeeping.Namespace)}: $namespace" : string.Empty;
        return $"({alias}:{Escape(definitionOfNode.Label)} {{{Escape(definitionOfNode.KeyProperty)}: ${keyParameter}{stamp}}})";
    }

    (string Match, Dictionary<string, object?> Parameters) EdgeMatch(EdgeReference edge)
    {
        var definitionOfEdge = definition.GetEdge(edge.Type);
        var parameters = EndpointParameters(edge);
        var where = string.Empty;
        if (edge.Identity is not null)
        {
            parameters["identity"] = Neo4jValues.ToDriver(edge.Identity);
            where = $" WHERE r.{Escape(definitionOfEdge.IdentifyingProperty!)} = $identity";
        }

        return ($"MATCH {NodePattern("s", edge.Source, "sourceKey")}-[r:{Escape(edge.Type)}]->{NodePattern("t", edge.Target, "targetKey")}{where}", parameters);
    }

    Dictionary<string, object?> EndpointParameters(EdgeReference edge)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["sourceKey"] = Neo4jValues.ToDriver(edge.Source.Key),
            ["targetKey"] = Neo4jValues.ToDriver(edge.Target.Key)
        };
        if (IsSingleDatabase) parameters["namespace"] = @namespace;
        return parameters;
    }

    Dictionary<string, object?> NodeParameters(NodeReference node, string keyParameter)
    {
        var parameters = new Dictionary<string, object?> { [keyParameter] = Neo4jValues.ToDriver(node.Key) };
        if (IsSingleDatabase) parameters["namespace"] = @namespace;
        return parameters;
    }

    Dictionary<string, object?> BaseProperties(NodeReference node)
    {
        var properties = new Dictionary<string, object?> { [definition.GetNode(node.Label).KeyProperty] = Neo4jValues.ToDriver(node.Key) };
        if (IsSingleDatabase) properties[GraphBookkeeping.Namespace] = @namespace;
        return properties;
    }

    Dictionary<string, object?> OwnerParameters(string owner)
    {
        var parameters = new Dictionary<string, object?> { ["owner"] = owner };
        if (IsSingleDatabase) parameters["namespace"] = @namespace;
        return parameters;
    }

    Dictionary<string, object?> SweepParameters(string owner, long generation)
    {
        var parameters = OwnerParameters(owner);
        parameters["generation"] = generation;
        return parameters;
    }

    Dictionary<string, object?> PageParameters(int skip, int take)
    {
        var parameters = new Dictionary<string, object?> { ["skip"] = (long)skip, ["take"] = (long)take };
        if (IsSingleDatabase) parameters["namespace"] = @namespace;
        return parameters;
    }
}
