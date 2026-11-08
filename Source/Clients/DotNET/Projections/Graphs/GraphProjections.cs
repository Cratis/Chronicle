// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Discovers the graphs an application declares, with the model-bound attributes and with the fluent builder, and builds what is registered for them.
/// </summary>
/// <param name="clientArtifacts">The <see cref="IClientArtifactsProvider"/> that finds the nodes, edges and graph projections.</param>
/// <param name="artifactsActivator">The <see cref="IClientArtifactsActivator"/> for creating the fluent graph projections.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for resolving event types.</param>
/// <param name="schemaGenerator">The <see cref="IJsonSchemaGenerator"/> for the schema of the node and edge types.</param>
/// <param name="sinkTypeId">The <see cref="SinkTypeId"/> of the sink that stores graphs.</param>
/// <param name="defaultSinkName">The name of the sink configuration graphs without a <see cref="SinkAttribute"/> store to; empty for the kernel's default.</param>
internal sealed class GraphProjections(
    IClientArtifactsProvider clientArtifacts,
    IClientArtifactsActivator artifactsActivator,
    INamingPolicy namingPolicy,
    IEventTypes eventTypes,
    IJsonSchemaGenerator schemaGenerator,
    SinkTypeId sinkTypeId,
    string defaultSinkName = "")
{
    /// <summary>
    /// Gets the registrations found by the last <see cref="Discover"/>.
    /// </summary>
    public IReadOnlyList<GraphRegistration> Registrations { get; private set; } = [];

    /// <summary>
    /// Gets the declarations that could not be turned into a registration, and why.
    /// </summary>
    public IDictionary<Type, Exception> Failures { get; } = new Dictionary<Type, Exception>();

    /// <summary>
    /// Discover the graphs.
    /// </summary>
    public void Discover()
    {
        Failures.Clear();
        var builder = new GraphDefinitionBuilder(namingPolicy, eventTypes, schemaGenerator);
        var registrations = new List<GraphRegistration>();
        foreach (var model in ModelBoundGraphs().Concat(FluentGraphs()))
        {
            try
            {
                registrations.Add(builder.Build(model.Model, model.ProjectionId, sinkTypeId.Value, SinkNameFor(model.Model, model.Declarations)));
            }
#pragma warning disable CA1031 // One invalid graph must not take the rest of the read side with it.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                Failures[model.Source] = exception;
            }
        }

        Registrations = registrations;
    }

    string SinkNameFor(GraphModel model, IEnumerable<Type> declarations)
    {
        var names = declarations.Select(_ => _.GetSinkName()).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (names.Length > 1) throw new GraphHasConflictingSinks(model.Name, names);
        return names.FirstOrDefault() ?? defaultSinkName ?? string.Empty;
    }

    IEnumerable<(Type Source, GraphModel Model, string ProjectionId, IEnumerable<Type> Declarations)> ModelBoundGraphs()
    {
        var reader = new ModelBoundGraphReader(namingPolicy);
        var nodes = new List<(string Graph, GraphNodeModel Model)>();
        var edges = new List<(string Graph, GraphEdgeModel Model)>();
        foreach (var type in clientArtifacts.GraphElements)
        {
            TryRead(type, () =>
            {
                if (type.IsDefined(typeof(NodeAttribute), false)) nodes.Add((ModelBoundGraphReader.GraphNameOf(type), reader.ReadNode(type)));
                if (type.IsDefined(typeof(EdgeAttribute), false)) edges.Add((ModelBoundGraphReader.GraphNameOf(type), reader.ReadEdge(type)));
            });
        }

        foreach (var graph in nodes.Select(_ => _.Graph).Concat(edges.Select(_ => _.Graph)).Distinct())
        {
            var model = new GraphModel(graph, [.. nodes.Where(_ => _.Graph == graph).Select(_ => _.Model)], [.. edges.Where(_ => _.Graph == graph).Select(_ => _.Model)]);
            var declarations = model.Nodes.Select(_ => _.Type).Concat(model.Edges.Select(_ => _.Type)).ToArray();
            yield return (declarations[0], model, graph, declarations);
        }
    }

    IEnumerable<(Type Source, GraphModel Model, string ProjectionId, IEnumerable<Type> Declarations)> FluentGraphs()
    {
        foreach (var type in clientArtifacts.GraphProjections)
        {
            GraphModel? model = null;
            if (!TryRead(type, () =>
            {
                var activated = artifactsActivator.ActivateNonDisposable<IGraphProjection>(type);
                if (activated.TryGetException(out var exception)) throw exception;
                model = new GraphProjectionBuilder(namingPolicy).Build(activated.AsT0);
            }))
            {
                continue;
            }

            yield return (type, model!, type.FullName ?? type.Name, [type]);
        }
    }

    bool TryRead(Type type, Action read)
    {
        try
        {
            read();
            return true;
        }
#pragma warning disable CA1031 // One invalid graph must not take the rest of the read side with it.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            Failures[type] = exception;
            return false;
        }
    }
}
