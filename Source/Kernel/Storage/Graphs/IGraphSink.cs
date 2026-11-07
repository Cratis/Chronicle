// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Defines the storage for the changes a projection makes to a property graph.
/// </summary>
/// <remarks>
/// <para>
/// A graph sink is separate from <see cref="ISink"/>: a projection onto a graph does not fold events into one document
/// per key, it applies operations to nodes and edges. The sink is created per graph, so several projections can write to
/// the same graph, each one owning what it writes (see <see cref="GraphChangeset.Owner"/>).
/// </para>
/// <para>
/// <b>Watermarks.</b> Every node and edge stores the event sequence number of its last write. A write whose event
/// sequence number is at or below the watermark of the element is skipped, which makes redelivered and out-of-order events
/// harmless. A node that an edge created without properties has no watermark yet, so the node projection can still
/// write it.
/// </para>
/// <para>
/// <b>Ownership and replay.</b> Writing an element stamps it with the owning projection and the replay generation of that
/// projection. A replay is mark-and-sweep: <see cref="BeginReplay"/> starts a new generation, an element of the owner that
/// is not yet stamped with it is stale and reads as absent, so the first write to it in the replay replaces it, and
/// <see cref="EndReplay"/> deletes every element of the owner that is still stale. Elements the projection never declared
/// are never touched, which includes nodes that edges created. A stale node that edges of other owners still point at is
/// not deleted but kept as a bare node, because an edge cannot exist without its end nodes.
/// </para>
/// <para>
/// <b>Reads while replaying.</b> <see cref="FindNode"/> and <see cref="FindEdge"/> are what the engine reads counters
/// from. During a replay they must report stale elements of the replaying owner as absent, otherwise a replay would
/// count on top of the previous result.
/// </para>
/// </remarks>
public interface IGraphSink
{
    /// <summary>
    /// Gets the <see cref="SinkTypeId"/> that identifies the sink.
    /// </summary>
    SinkTypeId TypeId { get; }

    /// <summary>
    /// Gets whether the sink supports more than one edge of a type between the same two nodes, as needed by edge types
    /// that declare an identifying property.
    /// </summary>
    bool SupportsMultipleEdges { get; }

    /// <summary>
    /// Ensure the schema of the graph exists: uniqueness constraints on the key of every node label and the indexes the
    /// graph definition needs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>Called when the graph is registered and is safe to call repeatedly.</remarks>
    Task EnsureSchema();

    /// <summary>
    /// Apply the operations one event caused.
    /// </summary>
    /// <param name="changeset">The <see cref="GraphChangeset"/> to apply.</param>
    /// <param name="eventSequenceNumber">The sequence number of the event that caused the changes. Used for the watermark.</param>
    /// <returns>The <see cref="FailedPartition">partitions</see> whose writes failed, if the sink buffers writes in a bulk window.</returns>
    Task<IEnumerable<FailedPartition>> ApplyGraphChanges(GraphChangeset changeset, EventSequenceNumber eventSequenceNumber);

    /// <summary>
    /// Find a node.
    /// </summary>
    /// <param name="owner">The <see cref="ProjectionId"/> reading it, which decides whether a stale element reads as absent during a replay.</param>
    /// <param name="node">The <see cref="NodeReference"/> to find.</param>
    /// <returns>The <see cref="GraphNode"/>, or null if it does not exist.</returns>
    Task<GraphNode?> FindNode(ProjectionId owner, NodeReference node);

    /// <summary>
    /// Find an edge.
    /// </summary>
    /// <param name="owner">The <see cref="ProjectionId"/> reading it, which decides whether a stale element reads as absent during a replay.</param>
    /// <param name="edge">The <see cref="EdgeReference"/> to find.</param>
    /// <returns>The <see cref="GraphEdge"/>, or null if it does not exist.</returns>
    Task<GraphEdge?> FindEdge(ProjectionId owner, EdgeReference edge);

    /// <summary>
    /// Begin bulk operation mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task BeginBulk();

    /// <summary>
    /// Join the open bulk window, or begin one when none is open.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ResumeBulk() => BeginBulk();

    /// <summary>
    /// Drain buffered writes while keeping the bulk window open.
    /// </summary>
    /// <returns>The window identity and unreported buffered write failures.</returns>
    Task<BulkFlushResult> FlushBulk() => Task.FromResult(new BulkFlushResult(null, []));

    /// <summary>
    /// End bulk operation mode.
    /// </summary>
    /// <returns>The partitions whose buffered writes failed, including those from the final flush.</returns>
    Task<IEnumerable<FailedPartition>> EndBulk();

    /// <summary>
    /// Start a replay for a projection: begin a new replay generation for the owner.
    /// </summary>
    /// <param name="owner">The <see cref="ProjectionId"/> that is replaying.</param>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay. Its start time identifies the generation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task BeginReplay(ProjectionId owner, ReplayContext context);

    /// <summary>
    /// Re-enter a replay that was already started, for example after a restart.
    /// </summary>
    /// <param name="owner">The <see cref="ProjectionId"/> that is replaying.</param>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ResumeReplay(ProjectionId owner, ReplayContext context);

    /// <summary>
    /// End a replay: sweep every element of the owner that was not written during the replay.
    /// </summary>
    /// <param name="owner">The <see cref="ProjectionId"/> that replayed.</param>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>The partitions whose writes failed during the final flush.</returns>
    Task<IEnumerable<FailedPartition>> EndReplay(ProjectionId owner, ReplayContext context);

    /// <summary>
    /// Leave a replay without sweeping, for a replay that was abandoned or already ended on another silo.
    /// </summary>
    /// <param name="owner">The <see cref="ProjectionId"/> that was replaying.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LeaveReplay(ProjectionId owner);

    /// <summary>
    /// Prepare the sink for an initial run of a projection by removing everything the owner wrote.
    /// </summary>
    /// <param name="owner">The <see cref="ProjectionId"/> to remove the elements of.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PrepareInitialRun(ProjectionId owner);

    /// <summary>
    /// Get nodes with a label, ordered by key.
    /// </summary>
    /// <param name="label">The node label.</param>
    /// <param name="skip">Number of nodes to skip.</param>
    /// <param name="take">Number of nodes to take.</param>
    /// <returns>The page of <see cref="GraphNode">nodes</see> and the total count.</returns>
    Task<GraphElements<GraphNode>> GetNodes(string label, int skip = 0, int take = 50);

    /// <summary>
    /// Get edges of a type, ordered by source key, target key and identity.
    /// </summary>
    /// <param name="type">The edge type.</param>
    /// <param name="skip">Number of edges to skip.</param>
    /// <param name="take">Number of edges to take.</param>
    /// <returns>The page of <see cref="GraphEdge">edges</see> and the total count.</returns>
    Task<GraphElements<GraphEdge>> GetEdges(string type, int skip = 0, int take = 50);
}
