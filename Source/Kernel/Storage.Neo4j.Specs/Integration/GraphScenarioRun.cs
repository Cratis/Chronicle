// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Neo4j.Integration;

/// <summary>
/// Represents one run of a scenario against a graph sink: the sink, helpers to apply and read, and what the scenario observed.
/// </summary>
/// <param name="sink">The <see cref="IGraphSink"/> the scenario runs against.</param>
public class GraphScenarioRun(IGraphSink sink)
{
    /// <summary>
    /// The owner the scenarios write as.
    /// </summary>
    public static readonly ProjectionId Owner = "owner";

    /// <summary>
    /// The owner of another projection.
    /// </summary>
    public static readonly ProjectionId OtherOwner = "other";

    readonly List<string> _observations = [];

    /// <summary>
    /// Gets the sink.
    /// </summary>
    public IGraphSink Sink => sink;

    /// <summary>
    /// Gets what the scenario observed along the way.
    /// </summary>
    public IReadOnlyList<string> Observations => _observations;

    /// <summary>
    /// Create a person node reference.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The <see cref="NodeReference"/>.</returns>
    public static NodeReference Person(string key) => new("Person", key);

    /// <summary>
    /// Create a group node reference.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The <see cref="NodeReference"/>.</returns>
    public static NodeReference Group(string key) => new("Group", key);

    /// <summary>
    /// Create a member of edge reference.
    /// </summary>
    /// <param name="person">The person key.</param>
    /// <param name="group">The group key.</param>
    /// <returns>The <see cref="EdgeReference"/>.</returns>
    public static EdgeReference MemberOf(string person, string group) => new("MEMBER_OF", Person(person), Group(group));

    /// <summary>
    /// Create a visited edge reference.
    /// </summary>
    /// <param name="person">The person key.</param>
    /// <param name="group">The group key.</param>
    /// <param name="identity">The identity.</param>
    /// <returns>The <see cref="EdgeReference"/>.</returns>
    public static EdgeReference Visited(string person, string group, string identity) => new("VISITED", Person(person), Group(group), identity);

    /// <summary>
    /// Create a replay context.
    /// </summary>
    /// <param name="seconds">The second the replay started at.</param>
    /// <returns>The <see cref="ReplayContext"/>.</returns>
    public static ReplayContext ReplayStartedAt(int seconds) =>
        new(new("graph", 1), "container", "revert", new DateTimeOffset(2026, 1, 1, 0, 0, seconds, TimeSpan.Zero));

    /// <summary>
    /// Create properties.
    /// </summary>
    /// <param name="properties">The properties by name.</param>
    /// <returns>The properties.</returns>
    public static IReadOnlyDictionary<string, object?> Props(params (string Name, object? Value)[] properties) =>
        properties.ToDictionary(_ => _.Name, _ => _.Value);

    /// <summary>
    /// Apply operations as the default owner.
    /// </summary>
    /// <param name="sequence">The event sequence number.</param>
    /// <param name="operations">The operations.</param>
    /// <returns>Awaitable task.</returns>
    public Task Apply(ulong sequence, params GraphOperation[] operations) => Apply(Owner, sequence, operations);

    /// <summary>
    /// Apply operations as an owner.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="sequence">The event sequence number.</param>
    /// <param name="operations">The operations.</param>
    /// <returns>Awaitable task.</returns>
    public Task Apply(ProjectionId owner, ulong sequence, params GraphOperation[] operations) =>
        sink.ApplyGraphChanges(new GraphChangeset(owner, operations), sequence);

    /// <summary>
    /// Apply operations and observe the error they cause, if any.
    /// </summary>
    /// <param name="name">What to call the observation.</param>
    /// <param name="sequence">The event sequence number.</param>
    /// <param name="operations">The operations.</param>
    /// <returns>Awaitable task.</returns>
    public async Task ApplyExpectingError(string name, ulong sequence, params GraphOperation[] operations)
    {
        var error = await Catch.Exception(() => Apply(sequence, operations));
        Observe(name, error?.GetType().Name ?? "no error");
    }

    /// <summary>
    /// Observe a node, as the default owner reads it.
    /// </summary>
    /// <param name="name">What to call the observation.</param>
    /// <param name="node">The node.</param>
    /// <returns>Awaitable task.</returns>
    public async Task ObserveNode(string name, NodeReference node) => Observe(name, GraphDump.Describe(await sink.FindNode(Owner, node)));

    /// <summary>
    /// Observe an edge, as the default owner reads it.
    /// </summary>
    /// <param name="name">What to call the observation.</param>
    /// <param name="edge">The edge.</param>
    /// <returns>Awaitable task.</returns>
    public async Task ObserveEdge(string name, EdgeReference edge) => Observe(name, GraphDump.Describe(await sink.FindEdge(Owner, edge)));

    /// <summary>
    /// Record an observation.
    /// </summary>
    /// <param name="name">What to call the observation.</param>
    /// <param name="value">What was observed.</param>
    public void Observe(string name, string value) => _observations.Add($"{name}: {value}");
}
