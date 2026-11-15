// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Compiles a graph declaration, a 'graph' and the 'projection ... => graph' that builds it, into definitions.
/// </summary>
/// <remarks>
/// Screenplay declares a graph projection in a slice next to its graph, so the declaration is compiled inside a
/// slice that Chronicle supplies. 'concept' declarations at the top level are kept at the top level.
/// </remarks>
internal static partial class GraphDeclarationLanguage
{
    const string WrapperIndent = "      ";
    static readonly string[] _wrapperHeader = ["module Chronicle", "  feature Graphs", "    slice StateView Graph"];

    [GeneratedRegex(@"^[ \t]*projection[ \t]+[\w.]+[ \t]*=>[ \t]*graph[ \t]+(?<graph>[\w.]+)", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex GraphProjectionPattern { get; }

    /// <summary>
    /// Gets whether a declaration holds a graph projection.
    /// </summary>
    /// <param name="declaration">The declaration text.</param>
    /// <returns>True if it declares 'projection ... => graph ...'.</returns>
    internal static bool IsGraphDeclaration(string declaration) => GraphProjectionPattern.IsMatch(declaration);

    /// <summary>
    /// Compiles a graph declaration.
    /// </summary>
    /// <param name="declaration">The declaration text.</param>
    /// <param name="owner">The <see cref="ProjectionOwner"/> of the projection.</param>
    /// <param name="eventTypeSchemas">The event type schemas used to resolve events and map properties by name.</param>
    /// <param name="generationDelivery">The <see cref="EventGenerationDelivery"/>.</param>
    /// <returns>The <see cref="CompiledGraph"/>, or the errors found.</returns>
    internal static (CompiledGraph? Graph, CompilerErrors Errors) Compile(
        string declaration,
        ProjectionOwner owner,
        IEnumerable<EventTypeSchema> eventTypeSchemas,
        EventGenerationDelivery generationDelivery)
    {
        var errors = new CompilerErrors();
        var (wrapped, lineMap) = Wrap(declaration);
        var result = new ScreenplayCompiler().Compile(wrapped);
        foreach (var diagnostic in result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error))
        {
            var line = diagnostic.Location.Line >= 1 && diagnostic.Location.Line <= lineMap.Count ? lineMap[diagnostic.Location.Line - 1] : 1;
            errors.Add(diagnostic.Message, line, Math.Max(1, diagnostic.Location.Column - WrapperIndent.Length));
        }

        var slice = result.Value?.Modules.FirstOrDefault()?.Features.FirstOrDefault()?.Slices.FirstOrDefault();
        if (errors.HasErrors || slice is null)
        {
            return (null, errors);
        }

        var projections = slice.GraphProjections.ToArray();
        if (projections.Length != 1)
        {
            errors.Add($"A graph declaration holds exactly one graph projection, found {projections.Length}", 1, 1);
            return (null, errors);
        }

        var projection = projections[0];
        var graph = slice.Graphs.FirstOrDefault(_ => _.Name == projection.Graph);
        if (graph is null)
        {
            errors.Add($"The graph '{projection.Graph}' is not declared; declare it with 'graph {projection.Graph}' in the same declaration", 1, 1);
            return (null, errors);
        }

        new ProjectionSyntaxSupport(errors).VisitGraphProjection(projection);
        if (errors.HasErrors)
        {
            return (null, errors);
        }

        var definition = new ProjectionDefinitionSyntaxVisitor(owner, eventTypeSchemas, generationDelivery).Visit(projection, graph);
        var readModel = ReadModelFor(graph, projection, result.Value!.Concepts);
        return (new CompiledGraph(definition, readModel), errors);
    }

    /// <summary>
    /// Gets the name of the graph a graph projection builds.
    /// </summary>
    /// <param name="declaration">The declaration text.</param>
    /// <returns>The graph name, or null when the declaration is not a graph projection.</returns>
    internal static string? GraphNameOf(string declaration) =>
        GraphProjectionPattern.Match(declaration) is { Success: true } match ? match.Groups["graph"].Value : null;

    static ReadModelDefinition ReadModelFor(GraphSyntax graph, GraphProjectionSyntax projection, IEnumerable<ConceptSyntax> concepts)
    {
        var primitives = concepts.ToDictionary(_ => _.Name, _ => _.Type);
        var properties = new JsonObject();
        foreach (var node in graph.Nodes)
        {
            var element = Element(node.Properties, primitives);
            element["x-graph-kind"] = "Node";
            element["x-graph-key"] = node.Properties.First(_ => _.IsKey).Name;
            properties[node.Label] = element;
        }

        foreach (var edge in graph.Edges)
        {
            var element = Element(edge.Properties, primitives);
            element["x-graph-kind"] = "Edge";
            element["x-graph-source"] = edge.Source;
            element["x-graph-target"] = edge.Target;
            if (edge.IdentifiedBy is not null)
            {
                element["x-graph-identity"] = edge.IdentifiedBy;
            }

            properties[edge.Type] = element;
        }

        var schema = new JsonObject { ["type"] = "object", ["title"] = graph.Name, ["x-graph"] = true, ["properties"] = properties };
        return new ReadModelDefinition(
            new ReadModelIdentifier(graph.Name),
            new ReadModelContainerName(graph.Name),
            new ReadModelDisplayName(graph.Name),
            ReadModelOwner.Server,
            ReadModelSource.User,
            ReadModelObserverType.Projection,
            new ReadModelObserverIdentifier(projection.Name),
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.Neo4j),
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, JsonSchema.FromJson(schema.ToJsonString()) } },
            []);
    }

    static JsonObject Element(IEnumerable<PropertySyntax> properties, Dictionary<string, string> concepts)
    {
        var result = new JsonObject();
        foreach (var property in properties)
        {
            var type = concepts.TryGetValue(property.Type.Name, out var primitive) ? primitive : property.Type.Name;
            var schema = PrimitiveSchema(type);
            if (property.Type.IsCollection)
            {
                schema = new JsonObject { ["type"] = "array", ["items"] = schema };
            }

            if (property.Type.IsOptional)
            {
                schema["type"] = new JsonArray("null", schema["type"]!.GetValue<string>());
            }

            result[property.Name] = schema;
        }

        return new JsonObject { ["type"] = "object", ["properties"] = result };
    }

    static JsonObject PrimitiveSchema(string primitive) => primitive switch
    {
        "Uuid" => new() { ["type"] = "string", ["format"] = "guid" },
        "Int" => new() { ["type"] = "integer", ["format"] = "int32" },
        "Decimal" => new() { ["type"] = "number", ["format"] = "decimal" },
        "Bool" => new() { ["type"] = "boolean" },
        "Date" => new() { ["type"] = "string", ["format"] = "date" },
        "DateTime" => new() { ["type"] = "string", ["format"] = "date-time" },
        _ => new() { ["type"] = "string" }
    };

    static (string Text, List<int> LineMap) Wrap(string declaration)
    {
        var source = declaration.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var concepts = new List<(string Text, int Line)>();
        var body = new List<(string Text, int Line)>();
        for (var index = 0; index < source.Length; index++)
        {
            var target = source[index].StartsWith("concept ", StringComparison.Ordinal) ? concepts : body;
            target.Add((source[index], index + 1));
        }

        var lines = new List<string>();
        var map = new List<int>();
        foreach (var (text, line) in concepts)
        {
            lines.Add(text);
            map.Add(line);
        }

        foreach (var header in _wrapperHeader)
        {
            lines.Add(header);
            map.Add(1);
        }

        foreach (var (text, line) in body)
        {
            lines.Add(text.Length == 0 ? text : WrapperIndent + text);
            map.Add(line);
        }

        return (string.Join('\n', lines) + '\n', map);
    }
}
