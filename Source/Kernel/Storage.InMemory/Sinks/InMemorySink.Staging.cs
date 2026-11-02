// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

public partial class InMemorySink
{
    static bool IsRootJoin(Change change) => change is Joined joined && joined.ArrayIndexers.IsEmpty;

    static bool IsChildJoin(Change change) => change is Joined joined && !joined.ArrayIndexers.IsEmpty;

    /// <summary>
    /// Evaluates every row an update touches into a <see cref="StagedWrite"/>, without publishing anything.
    /// </summary>
    /// <param name="key">The <see cref="Key"/> the changeset is for.</param>
    /// <param name="changeset">The changeset to evaluate.</param>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> being applied.</param>
    /// <param name="mode">The <see cref="SinkWriteMode"/> of the keyed write.</param>
    /// <returns>The staged write, ready to publish.</returns>
    /// <remarks>Callers must hold <c language="csharp">_collectionLock</c>.</remarks>
    StagedWrite Stage(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkWriteMode mode)
    {
        var staged = new StagedWrite(Collection);
        var changes = changeset.Changes.ToArray();
        var removed = changeset.HasBeenRemoved();
        var rootJoins = changes.Where(IsRootJoin).Cast<Joined>().ToArray();
        var onlyRootJoins = rootJoins.Length > 0 && rootJoins.All(joined => !joined.HasKeyedFrom);
        if (!removed && onlyRootJoins)
        {
            // Root joins enrich existing rows selected by their join column, not by the incoming
            // event source id. Never create a phantom row for a join-only event. In particular,
            // the root all-event mapper is inside the Joined change, not a keyed From change.
            StageRootJoins(staged, rootJoins);
            return staged;
        }

        var keyValue = GetKeyValue(key);
        if (removed)
        {
            if (!IsRejectedGuardedRemoval(keyValue, eventSequenceNumber, mode))
            {
                staged.StageRemoval(keyValue);
            }

            return staged;
        }

        var keyedWrite = new KeyedWrite(key, keyValue, IsRejectedWatermarkWrite(keyValue, eventSequenceNumber, mode));
        StageKeyedChanges(staged, keyedWrite, changes, changeset.InitialState.Clone());
        return staged;
    }

    void StageKeyedChanges(StagedWrite staged, KeyedWrite keyedWrite, IReadOnlyList<Change> changes, ExpandoObject state)
    {
        var segments = ChangesetWriteOrder.SegmentsFor(changes) ?? [changes];
        for (var index = 0; index < segments.Count; index++)
        {
            state = StageSegment(staged, keyedWrite with { ContinuesEarlierSegment = index > 0 }, segments[index], state);
        }
    }

    ExpandoObject StageSegment(StagedWrite staged, KeyedWrite keyedWrite, IReadOnlyList<Change> changes, ExpandoObject state)
    {
        var rootJoins = changes.Where(IsRootJoin).Cast<Joined>().ToArray();
        var childJoins = changes.Where(IsChildJoin).Cast<Joined>().ToArray();
        var removals = changes.OfType<ChildRemovedFromAll>().ToArray();
        StageRootJoins(staged, rootJoins);

        // Child joins enrich every stored parent holding the child, even when the changeset's initial state is empty,
        // and are applied before a removal takes the child away.
        StageChildJoins(staged, childJoins);

        // Removing a child from every model that matches spans documents, so it cannot be expressed by
        // applying changes to the one being written - the persistent sinks run it across the container.
        StageRemovalsFromAll(staged, removals);
        foreach (var removal in removals)
        {
            RemoveChildFromDocument(state, removal);
        }

        // The joined update may have touched this same row. Apply only the direct changes
        // on top of the now-current row, without replaying the join against the From key.
        var joinsMayHaveTouchedTheRow = keyedWrite.ContinuesEarlierSegment || rootJoins.Any(joined => joined.HasKeyedFrom) || childJoins.Length > 0;
        if (joinsMayHaveTouchedTheRow && staged.Current(keyedWrite.KeyValue) is not null)
        {
            state = staged.CandidateFor(keyedWrite.KeyValue);
        }

        return StageDirectChanges(staged, keyedWrite, changes, state);
    }

    ExpandoObject StageDirectChanges(StagedWrite staged, KeyedWrite keyedWrite, IReadOnlyList<Change> changes, ExpandoObject state)
    {
        var directChanges = changes.Where(change => change is not Joined).ToArray();
        var childJoinOnly = changes.Any(IsChildJoin) && directChanges.Length == 0;
        if (childJoinOnly || keyedWrite.IsRejected)
        {
            return state;
        }

        state = ApplyActualChanges(keyedWrite.Key, directChanges, state);
        ((IDictionary<string, object?>)state)["id"] = Detached(keyedWrite.Key.Value);
        staged.Stage(keyedWrite.KeyValue, state, advancesWatermark: true);
        return state;
    }

    void StageRootJoins(StagedWrite staged, Joined[] rootJoins)
    {
        if (rootJoins.Length == 0)
        {
            return;
        }

        foreach (var rowKey in staged.RowKeys())
        {
            var current = staged.Current(rowKey)!;
            var matching = rootJoins.Where(joined =>
                joined.Changes.Any() && TryFindValueInDocument(current, joined.OnProperty.Segments.ToArray(), 0, joined.Key)).ToArray();
            if (matching.Length == 0)
            {
                continue;
            }

            var candidate = staged.CandidateFor(rowKey);
            foreach (var joined in matching)
            {
                candidate = ApplyActualChanges(new Key(rowKey, ArrayIndexers.NoIndexers), joined.Changes, candidate);
            }

            staged.Stage(rowKey, candidate, advancesWatermark: true);
        }
    }

    void StageChildJoins(StagedWrite staged, Joined[] childJoins)
    {
        foreach (var joined in childJoins)
        {
            var indexer = joined.ArrayIndexers.All.Last();
            var childPath = (indexer.ArrayProperty + indexer.IdentifierProperty).Segments.ToArray();
            foreach (var rowKey in staged.RowKeys())
            {
                if (!joined.Changes.Any() || !TryFindValueInDocument(staged.Current(rowKey)!, childPath, 0, indexer.Identifier))
                {
                    continue;
                }

                var candidate = ApplyActualChanges(new Key(rowKey, joined.ArrayIndexers), joined.Changes, staged.CandidateFor(rowKey));
                staged.Stage(rowKey, candidate, advancesWatermark: true);
            }
        }
    }

    void StageRemovalsFromAll(StagedWrite staged, ChildRemovedFromAll[] removals)
    {
        foreach (var removal in removals)
        {
            foreach (var rowKey in staged.RowKeys().Where(rowKey => HoldsChild(staged.Current(rowKey)!, removal)))
            {
                var candidate = staged.CandidateFor(rowKey);
                RemoveChildFromDocument(candidate, removal);
                staged.Stage(rowKey, candidate, advancesWatermark: false);
            }
        }
    }

    /// <summary>
    /// Publishes a fully evaluated <see cref="StagedWrite"/>.
    /// </summary>
    /// <param name="staged">The staged write.</param>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> being applied.</param>
    /// <remarks>Callers must hold <c language="csharp">_collectionLock</c>, the same lock the write was staged under.</remarks>
    void Publish(StagedWrite staged, EventSequenceNumber eventSequenceNumber)
    {
        if (staged.RemovedKey is { } removedKey)
        {
            Collection.Remove(removedKey);
            LastHandledEventSequenceNumbers.Remove(removedKey);
        }

        foreach (var (rowKey, row, advancesWatermark) in staged.Candidates())
        {
            Collection[rowKey] = row;
            if (advancesWatermark)
            {
                AdvanceWatermark(rowKey, eventSequenceNumber);
            }
        }
    }

    sealed record KeyedWrite(Key Key, object KeyValue, bool IsRejected)
    {
        public bool ContinuesEarlierSegment { get; init; }
    }
}
