// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Graphs;
using Cratis.Serialization;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphViews"/>.
/// </summary>
public sealed class GraphViews : IGraphViews
{
    readonly IReadOnlyDictionary<Type, GraphViewTarget> _targets;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphViews"/> class.
    /// </summary>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> names were given with.</param>
    /// <param name="definitions">The definitions of the valid views.</param>
    /// <param name="targets">The valid view types with where they read.</param>
    /// <param name="failures">The views that were left out.</param>
    internal GraphViews(
        INamingPolicy namingPolicy,
        IReadOnlyList<GraphViewDefinition> definitions,
        IReadOnlyDictionary<Type, GraphViewTarget> targets,
        IReadOnlyDictionary<Type, Exception> failures)
    {
        NamingPolicy = namingPolicy;
        Definitions = definitions;
        _targets = targets;
        Failures = failures;
    }

    /// <summary>
    /// Gets an instance without any views.
    /// </summary>
    public static GraphViews Empty { get; } = new(new CamelCaseNamingPolicy(), [], new Dictionary<Type, GraphViewTarget>(), new Dictionary<Type, Exception>());

    /// <inheritdoc/>
    public IReadOnlyList<GraphViewDefinition> Definitions { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Type, Exception> Failures { get; }

    /// <inheritdoc/>
    public INamingPolicy NamingPolicy { get; }

    /// <summary>
    /// Discover the views and the graphs they read from client artifacts, the way the client does.
    /// </summary>
    /// <param name="clientArtifacts">The <see cref="IClientArtifactsProvider"/> to discover from.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/>; camelCase when not given.</param>
    /// <returns>The <see cref="GraphViews"/>.</returns>
    /// <remarks>
    /// Graphs declared with the node and edge attributes are read, and graph projections declared with the fluent builder
    /// when they have a public parameterless constructor. A graph that cannot be read is skipped; the client reports it
    /// when it registers its graphs.
    /// </remarks>
    public static GraphViews Discover(IClientArtifactsProvider clientArtifacts, INamingPolicy? namingPolicy = null)
    {
        namingPolicy ??= new CamelCaseNamingPolicy();
        return new GraphViewReader(namingPolicy).Read(clientArtifacts.GraphViews, ReadGraphs(clientArtifacts, namingPolicy));
    }

    /// <inheritdoc/>
    public bool IsView(Type viewType) => _targets.ContainsKey(viewType);

    /// <inheritdoc/>
    public string GraphOf(Type viewType) => TargetOf(viewType).Graph;

    /// <inheritdoc/>
    public GraphQueryNode NodeOf(Type viewType) => TargetOf(viewType).Node;

    /// <inheritdoc/>
    public GraphQueryNode NodeOf(string graph, string view) =>
        _targets.Values.FirstOrDefault(_ => _.Graph == graph && _.Node.View == view)?.Node ?? throw new UnknownGraphView(graph, view);

    static List<GraphModel> ReadGraphs(IClientArtifactsProvider clientArtifacts, INamingPolicy namingPolicy)
    {
        var reader = new ModelBoundGraphReader(namingPolicy);
        var nodes = new List<(string Graph, GraphNodeModel Model)>();
        var edges = new List<(string Graph, GraphEdgeModel Model)>();
        foreach (var type in clientArtifacts.GraphElements)
        {
            TryRead(() =>
            {
                if (type.IsDefined(typeof(NodeAttribute), false)) nodes.Add((ModelBoundGraphReader.GraphNameOf(type), reader.ReadNode(type)));
                if (type.IsDefined(typeof(EdgeAttribute), false)) edges.Add((ModelBoundGraphReader.GraphNameOf(type), reader.ReadEdge(type)));
            });
        }

        var models = nodes.Select(_ => _.Graph).Concat(edges.Select(_ => _.Graph)).Distinct()
            .Select(graph => new GraphModel(graph, [.. nodes.Where(_ => _.Graph == graph).Select(_ => _.Model)], [.. edges.Where(_ => _.Graph == graph).Select(_ => _.Model)]))
            .ToList();

        foreach (var type in clientArtifacts.GraphProjections.Where(_ => _.GetConstructor(Type.EmptyTypes) is not null))
        {
            TryRead(() => models.Add(new GraphProjectionBuilder(namingPolicy).Build((IGraphProjection)Activator.CreateInstance(type)!)));
        }

        return models;
    }

    static bool TryRead(Action read)
    {
        try
        {
            read();
            return true;
        }
#pragma warning disable CA1031 // A graph that cannot be read is reported when the client registers its graphs, not here.
        catch (Exception)
#pragma warning restore CA1031
        {
            // Skipped on purpose: see the remark on Discover.
            return false;
        }
    }

    GraphViewTarget TargetOf(Type viewType) =>
        _targets.TryGetValue(viewType, out var target) ? target : throw new NotAGraphView(viewType, Failures.GetValueOrDefault(viewType));
}
