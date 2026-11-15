// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;

namespace Cratis.Chronicle.Storage.Neo4j.Integration;

/// <summary>
/// The scenarios of the in-memory graph sink specifications, written so they run against any <see cref="IGraphSink"/>.
/// </summary>
/// <remarks>
/// Each scenario mirrors a specification in <c language="text">Storage.InMemory.Specs/Sinks/Graphs</c>, and is named after it.
/// What each scenario observes along the way is recorded, and the graph it leaves behind is described afterwards, so the
/// integration specs can require the same result from the live sink as from the reference.
/// </remarks>
public static class GraphScenarios
{
    /// <summary>
    /// Gets all scenarios by name.
    /// </summary>
    public static IReadOnlyDictionary<string, Func<GraphScenarioRun, Task>> All { get; } = new Dictionary<string, Func<GraphScenarioRun, Task>>
    {
        ["applying a changeset that refers to an unknown label"] = UnknownLabel,
        ["deleting a node with detach"] = DeleteWithDetach,
        ["deleting a node without detach and no edges"] = DeleteWithoutDetachAndNoEdges,
        ["deleting a node without detach while it has edges"] = DeleteWithoutDetachWithEdges,
        ["deleting an edge"] = DeleteEdge,
        ["upserting a node that does not exist"] = UpsertNew,
        ["upserting a node that exists"] = UpsertExisting,
        ["upserting a node with an older event arriving late"] = UpsertOlderEvent,
        ["upserting a node with a redelivered event"] = UpsertRedelivered,
        ["upserting a node an edge created"] = UpsertNodeCreatedByEdge,
        ["upserting an edge with an identifying property"] = UpsertEdgeWithIdentity,
        ["upserting an edge without an identifying property"] = UpsertEdgeWithoutIdentity,
        ["upserting an edge whose end nodes do not exist"] = UpsertEdgeCreatingNodes,
        ["setting properties on a node"] = SetOnNode,
        ["setting properties on an edge"] = SetOnEdge,
        ["replaying and rewriting a stale element"] = ReplayRewritesStale,
        ["replaying and sweeping what was not written"] = ReplaySweeps,
        ["replaying and leaving without ending"] = ReplayLeft,
        ["replaying a node other projections still have edges to"] = ReplayKeepsNodeWithForeignEdges,
        ["preparing an initial run"] = InitialRun
    };

    static async Task UnknownLabel(GraphScenarioRun run)
    {
        await run.ApplyExpectingError(
            "refused",
            1,
            new UpsertNode(Person("p1"), Props(("name", "Ada"))),
            new UpsertNode(new("Robot", "r1"), Props()));
        await run.ObserveNode("person", Person("p1"));
    }

    static async Task DeleteWithDetach(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))), new UpsertEdge(MemberOf("p1", "g1"), Props()));
        await run.Apply(2, new DeleteNode(Person("p1"), true));
        await run.ObserveNode("node", Person("p1"));
        await run.ObserveNode("group", Group("g1"));
        await run.ObserveEdge("edge", MemberOf("p1", "g1"));
    }

    static async Task DeleteWithoutDetachAndNoEdges(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        await run.Apply(1, new DeleteNode(Person("p1"), false));
        await run.ObserveNode("after the delete at the watermark", Person("p1"));
        await run.Apply(2, new DeleteNode(Person("p1"), false));
        await run.ObserveNode("after the later delete", Person("p1"));
    }

    static async Task DeleteWithoutDetachWithEdges(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))), new UpsertEdge(MemberOf("p1", "g1"), Props()));
        await run.ApplyExpectingError("refused", 2, new DeleteNode(Person("p1"), false));
        await run.ObserveNode("node", Person("p1"));
    }

    static async Task DeleteEdge(GraphScenarioRun run)
    {
        await run.Apply(3, new UpsertEdge(MemberOf("p1", "g1"), Props()));
        await run.Apply(2, new DeleteEdge(MemberOf("p1", "g1")));
        await run.ObserveEdge("after the delete below the watermark", MemberOf("p1", "g1"));
        await run.Apply(4, new DeleteEdge(MemberOf("p1", "g1")));
        await run.ObserveEdge("after the later delete", MemberOf("p1", "g1"));
        await run.ObserveNode("source", Person("p1"));
    }

    static async Task UpsertNew(GraphScenarioRun run)
    {
        await run.Apply(5, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"))));
        await run.ObserveNode("node", Person("p1"));
    }

    static async Task UpsertExisting(GraphScenarioRun run)
    {
        await run.Apply(5, new UpsertNode(Person("p1"), Props(("name", "Ada"), ("visits", 1))));
        await run.Apply(6, new UpsertNode(Person("p1"), Props(("name", "Ada L."), ("visits", null))));
        await run.ObserveNode("node", Person("p1"));
    }

    static async Task UpsertOlderEvent(GraphScenarioRun run)
    {
        await run.Apply(9, new UpsertNode(Person("p1"), Props(("name", "Newer"))));
        await run.Apply(4, new UpsertNode(Person("p1"), Props(("name", "Older"))));
        await run.ObserveNode("node", Person("p1"));
    }

    static async Task UpsertRedelivered(GraphScenarioRun run)
    {
        await run.Apply(5, new UpsertNode(Person("p1"), Props(("visits", 1))));
        await run.Apply(5, new UpsertNode(Person("p1"), Props(("visits", 2))));
        await run.ObserveNode("node", Person("p1"));
    }

    static async Task UpsertNodeCreatedByEdge(GraphScenarioRun run)
    {
        await run.Apply(9, new UpsertEdge(MemberOf("p1", "g1"), Props()));
        await run.ObserveNode("bare node", Person("p1"));
        await run.Apply(4, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        await run.ObserveNode("completed node", Person("p1"));
    }

    static async Task UpsertEdgeWithIdentity(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertEdge(Visited("p1", "g1", "a"), Props(("count", 1))));
        await run.Apply(2, new UpsertEdge(Visited("p1", "g1", "b"), Props(("count", 1))));
        await run.Apply(3, new UpsertEdge(Visited("p1", "g1", "a"), Props(("count", 2))));
        await run.ApplyExpectingError("without identity", 4, new UpsertEdge(new("VISITED", Person("p1"), Group("g1")), Props()));
    }

    static async Task UpsertEdgeWithoutIdentity(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2020"))));
        await run.Apply(2, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2021"))));
        await run.Apply(2, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2022"))));
    }

    static async Task UpsertEdgeCreatingNodes(GraphScenarioRun run)
    {
        await run.Apply(3, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2020"))));
        await run.ObserveEdge("edge", MemberOf("p1", "g1"));
        await run.ObserveNode("source", Person("p1"));
        await run.ObserveNode("target", Group("g1"));
    }

    static async Task SetOnNode(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        await run.Apply(2, new SetProperties(Person("p1"), Props(("visits", 3))), new SetProperties(Person("nobody"), Props(("name", "X"))));
        await run.ObserveNode("node", Person("p1"));
        await run.ObserveNode("missing", Person("nobody"));
    }

    static async Task SetOnEdge(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2020"))));
        await run.Apply(2, new SetProperties(MemberOf("p1", "g1"), Props(("since", "2021"))));
        await run.Apply(1, new SetProperties(MemberOf("p1", "g1"), Props(("since", "1999"))));
        await run.ObserveEdge("edge", MemberOf("p1", "g1"));
    }

    static async Task ReplayRewritesStale(GraphScenarioRun run)
    {
        await run.Apply(50, new UpsertNode(Person("p1"), Props(("name", "Ada"), ("visits", 7))));
        var context = ReplayStartedAt(10);
        await run.Sink.BeginReplay(Owner, context);
        await run.ObserveNode("during the replay before the write", Person("p1"));
        await run.Apply(1, new UpsertNode(Person("p1"), Props(("visits", 1))));
        await run.Sink.EndReplay(Owner, context);
        await run.ObserveNode("after the replay", Person("p1"));
    }

    static async Task ReplaySweeps(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertNode(Person("kept"), Props(("name", "Old"))), new UpsertNode(Person("stale"), Props(("name", "Gone"))));
        await run.Apply(2, new UpsertEdge(MemberOf("stale", "g1"), Props()));
        await run.Apply(OtherOwner, 3, new UpsertNode(Person("theirs"), Props(("name", "Theirs"))));
        var context = ReplayStartedAt(10);
        await run.Sink.BeginReplay(Owner, context);
        await run.Apply(1, new UpsertNode(Person("kept"), Props(("name", "New"))));
        await run.Sink.EndReplay(Owner, context);
        await run.ObserveNode("kept", Person("kept"));
        await run.ObserveNode("stale", Person("stale"));
        await run.ObserveEdge("stale edge", MemberOf("stale", "g1"));
        await run.ObserveNode("theirs", Person("theirs"));
        await run.ObserveNode("bare", Group("g1"));
    }

    static async Task ReplayLeft(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        await run.Sink.BeginReplay(Owner, ReplayStartedAt(10));
        await run.Sink.LeaveReplay(Owner);
        await run.ObserveNode("node", Person("p1"));
    }

    static async Task ReplayKeepsNodeWithForeignEdges(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        await run.Apply(OtherOwner, 2, new UpsertEdge(MemberOf("p1", "g1"), Props()));
        var context = ReplayStartedAt(10);
        await run.Sink.BeginReplay(Owner, context);
        await run.Sink.EndReplay(Owner, context);
        await run.ObserveNode("node", Person("p1"));
        await run.ObserveEdge("edge", MemberOf("p1", "g1"));
    }

    static async Task InitialRun(GraphScenarioRun run)
    {
        await run.Apply(1, new UpsertNode(Person("mine"), Props(("name", "Ada"))));
        await run.Apply(OtherOwner, 2, new UpsertNode(Person("theirs"), Props(("name", "Bob"))));
        await run.Sink.PrepareInitialRun(Owner);
        await run.ObserveNode("mine", Person("mine"));
        await run.ObserveNode("theirs", Person("theirs"));
    }
}
