// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelKey = KernelConcepts::Cratis.Chronicle.Concepts.Keys.Key;
using KernelProjectionEngine = KernelCore::Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Resolves the root read-model document a <c>[Join]</c> source event should enrich when a projection is driven
/// through the in-memory test harness.
/// </summary>
/// <remarks>
/// The production engine's <c>ForJoin</c> key resolver targets a real sink (MongoDB), so a join source resolves
/// to the root document that actually holds the joined value. Run against the in-memory sink the same resolver
/// instead yields a key whose value is the join source's OWN event source id — materializing a phantom document
/// (root joins) or duplicating a child (child joins). This collaborator recovers the true root by matching the
/// join value in the sink (<see cref="InMemorySink.TryFindRootKeyByChildValue"/>), mirroring what the real
/// engine/Mongo does, without changing any engine behavior. It owns only the harness's join-key-resolution
/// concern; <c>ProcessSingleEvent</c> stays orchestration.
/// </remarks>
internal static class JoinKeyResolver
{
    /// <summary>
    /// Resolves the root read-model key for a root-level <c>[Join]</c> source event by matching the join's
    /// <c>On</c> property against the join event's own event source id — the same lookup the production engine
    /// performs against its sink, applied here uniformly for any key type (Guid, string concept, and so on).
    /// </summary>
    /// <param name="projection">The <see cref="KernelProjectionEngine::IProjection"/> being processed.</param>
    /// <param name="kernelProjectionDefinition">The kernel projection definition carrying the join metadata.</param>
    /// <param name="sink">The in-memory sink holding the materialized root documents.</param>
    /// <param name="event">The event being processed.</param>
    /// <returns>
    /// A tuple whose <c>IsRootJoinSource</c> is <see langword="true"/> only when <paramref name="event"/> is a
    /// root-level join source (so the caller must resolve here rather than defer to the engine's key resolver).
    /// <c>RootKey</c> is the matched root document key to enrich, or <see langword="null"/> when no root row
    /// exists yet (skip without writing a phantom).
    /// </returns>
    /// <remarks>
    /// The engine's own <c>ForJoin</c> key resolver targets its real sink; run against the in-memory sink it
    /// yields a phantom document keyed by the join source's id rather than enriching the existing root. This
    /// replicates only the intended root lookup (<c>sink.TryFindRootKeyByChildValue</c> on the join <c>On</c>
    /// column), so no engine behavior changes — a nested join or a non-join event returns
    /// <c>IsRootJoinSource = false</c> and falls through to the engine's normal resolution.
    /// </remarks>
    public static async Task<(bool IsRootJoinSource, KernelKey? RootKey)> TryResolveRootJoinKey(
        KernelProjectionEngine::IProjection projection,
        KernelConceptsNs::Projections.Definitions.ProjectionDefinition kernelProjectionDefinition,
        InMemorySink sink,
        KernelAppendedEvent @event)
    {
        if (projection.HasParent)
        {
            return (false, null);
        }

        // Only a pure, root-level join source routes here. An event that also carries a From mapping
        // creates/updates the row itself (the engine keeps its From key resolver for it), and a join that
        // affects a child collection is handled by the child-join re-anchoring — so leave both to normal
        // resolution; skipping a From event would drop the row it is responsible for creating.
        var operationType = projection.GetOperationTypeFor(@event.Context.EventType);
        if (!operationType.HasFlag(KernelProjectionEngine::ProjectionOperationType.Join) ||
            operationType.HasFlag(KernelProjectionEngine::ProjectionOperationType.From) ||
            operationType.HasFlag(KernelProjectionEngine::ProjectionOperationType.ChildrenAffected))
        {
            return (false, null);
        }

        var joinDefinition = kernelProjectionDefinition.Join
            .FirstOrDefault(join => join.Key.Id == @event.Context.EventType.Id).Value;
        if (joinDefinition is null)
        {
            return (false, null);
        }

        var rootKeyResult = await sink.TryFindRootKeyByChildValue(joinDefinition.On, @event.Context.EventSourceId.Value);
        return rootKeyResult.TryGetValue(out var rootKey) ? (true, rootKey) : (true, null);
    }

    /// <summary>
    /// Resolves the root read-model key for a child-level <c>[Join]</c> source event whose engine-resolved key
    /// carries array indexers into a child collection — by locating the root document that contains the matching
    /// child (via the child value in the sink), mirroring the production engine's behavior against its real sink.
    /// </summary>
    /// <param name="projection">The <see cref="KernelProjectionEngine::IProjection"/> being processed.</param>
    /// <param name="sink">The in-memory sink holding the materialized root documents.</param>
    /// <param name="event">The event being processed.</param>
    /// <param name="key">The key the engine resolved for <paramref name="event"/>, carrying the child array indexers.</param>
    /// <returns>
    /// A tuple whose <c>IsChildJoinSource</c> is <see langword="true"/> only when <paramref name="event"/> is a
    /// child-level join source (the caller must re-anchor onto the returned root, keeping the array indexers).
    /// <c>RootKey</c> is the matched root document key whose child to enrich, or <see langword="null"/> when no
    /// root contains the child yet (skip without writing a phantom).
    /// </returns>
    /// <remarks>
    /// The engine's <c>ForJoin</c> child branch resolves the key value to the join source's OWN event source id
    /// with an array indexer into the child collection; against the in-memory sink that would write a phantom
    /// document keyed by that id and duplicate the child. The array indexer names the child collection and the
    /// child key, so <c>sink.TryFindRootKeyByChildValue</c> on that path recovers the true root — no engine
    /// behavior changes, and a non-child-join event (no array indexers or no Join flag) returns
    /// <c>IsChildJoinSource = false</c> and falls through to normal handling.
    /// </remarks>
    public static async Task<(bool IsChildJoinSource, KernelKey? RootKey)> TryResolveChildJoinRootKey(
        KernelProjectionEngine::IProjection projection,
        InMemorySink sink,
        KernelAppendedEvent @event,
        KernelKey key)
    {
        if (key.ArrayIndexers.IsEmpty ||
            !projection.GetOperationTypeFor(@event.Context.EventType).HasFlag(KernelProjectionEngine::ProjectionOperationType.Join))
        {
            return (false, null);
        }

        // The leaf array indexer names the child collection and identifies the child within it; the full dotted
        // path from the root to the child key (e.g. "members.memberId") is what the sink matches against.
        var indexers = key.ArrayIndexers.All.ToList();
        var leaf = indexers[^1];
        var childPath = new PropertyPath(string.Join('.', indexers.Select(indexer => indexer.ArrayProperty.Path).Append(leaf.IdentifierProperty.Path)));

        var rootKeyResult = await sink.TryFindRootKeyByChildValue(childPath, leaf.Identifier);
        return rootKeyResult.TryGetValue(out var rootKey) ? (true, rootKey) : (true, null);
    }
}
