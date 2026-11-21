// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Writes a <see cref="GraphViewDefinition"/> in its canonical JSON form, the form every Chronicle client produces for
/// the same declaration.
/// </summary>
/// <remarks>
/// There are two forms of the same definition: the default form <c language="json">{ graph, views }</c>, and the schema-included form
/// <c language="json">{ graph, schema, views }</c> that also lists the nodes and edges the views were checked against, as the shared
/// conformance fixture does. The canonical form is UTF-8 with LF line endings, two-space indentation and one trailing newline. Views are sorted by
/// name, ordinal ascending, and members keep their declaration order. Each member is written on one line. Enumerations
/// are written in camelCase.
/// </remarks>
public static class GraphViewDefinitionSerializer
{
    /// <summary>
    /// Serialize a definition to its canonical JSON form.
    /// </summary>
    /// <param name="definition">The <see cref="GraphViewDefinition"/> to serialize.</param>
    /// <param name="includeSchema">Whether to write the schema-included form.</param>
    /// <returns>The canonical JSON.</returns>
    /// <exception cref="GraphViewDefinitionHasNoSchema">Thrown when the schema-included form is asked for and the definition has no schema.</exception>
    public static string Serialize(GraphViewDefinition definition, bool includeSchema = false)
    {
        var schema = includeSchema
            ? $"  \"schema\": {Schema(definition.Schema ?? throw new GraphViewDefinitionHasNoSchema(definition.Graph))},\n"
            : string.Empty;
        var views = definition.Views.OrderBy(_ => _.Name, StringComparer.Ordinal).Select(View);
        return $"{{\n  \"graph\": {Quote(definition.Graph)},\n{schema}  \"views\": {Block(views, "  ")}\n}}\n";
    }

    static string Schema(GraphViewSchema schema)
    {
        var nodes = schema.Nodes.Select(node => Inline([("label", Quote(node.Label)), ("key", Quote(node.Key)), ("properties", InlineList(node.Properties))]));
        var edges = schema.Edges.Select(edge =>
        {
            var pairs = new List<(string, string)> { ("type", Quote(edge.Type)), ("source", Quote(edge.Source)), ("target", Quote(edge.Target)) };
            if (edge.IdentifiedBy is not null) pairs.Add(("identifiedBy", Quote(edge.IdentifiedBy)));
            pairs.Add(("properties", InlineList(edge.Properties)));
            return Inline(pairs);
        });

        return $"{{\n    \"nodes\": {Block(nodes.Select(_ => $"      {_}"), "    ")},\n    \"edges\": {Block(edges.Select(_ => $"      {_}"), "    ")}\n  }}";
    }

    static string View(GraphView view)
    {
        var members = Block(view.Members.Select(_ => $"        {Member(_)}"), "      ");
        return $"    {{\n      \"name\": {Quote(view.Name)},\n      \"root\": {Quote(view.Root)},\n      \"members\": {members}\n    }}";
    }

    static string Block(IEnumerable<string> items, string closingIndentation)
    {
        var all = items.ToArray();
        return all.Length == 0 ? "[]" : $"[\n{string.Join(",\n", all)}\n{closingIndentation}]";
    }

    static string Inline(IEnumerable<(string Name, string Value)> pairs) => $"{{ {string.Join(", ", pairs.Select(_ => $"{Quote(_.Name)}: {_.Value}"))} }}";

    static string InlineList(IReadOnlyList<string> values) => values.Count == 0 ? "[]" : $"[ {string.Join(", ", values.Select(Quote))} ]";

    static string Member(GraphViewMember member)
    {
        var properties = new List<(string Name, string Value)>
        {
            ("name", member.Name),
            ("kind", KindOf(member.Kind))
        };

        if (member.Kind == GraphViewMemberKind.Relation)
        {
            properties.Add(("edge", member.Edge ?? string.Empty));
            properties.Add(("direction", member.Direction == GraphViewDirection.Incoming ? "incoming" : "outgoing"));
            properties.Add(("cardinality", member.Cardinality == GraphViewCardinality.One ? "one" : "many"));
            properties.Add(("view", member.View ?? string.Empty));
        }
        else
        {
            properties.Add(("source", member.Source ?? string.Empty));
        }

        return Inline(properties.Select(_ => (_.Name, Quote(_.Value))));
    }

    static string KindOf(GraphViewMemberKind kind) => kind switch
    {
        GraphViewMemberKind.Relation => "relation",
        GraphViewMemberKind.EdgeProperty => "edgeProperty",
        _ => "property"
    };

    static string Quote(string value) => $"\"{JsonEncodedText.Encode(value)}\"";
}
