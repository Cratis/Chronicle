// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    readonly Dictionary<ConstraintName, int> _rebuildingConstraintIndexes = [];
    readonly Dictionary<ConstraintIndexRebuildRunId, ConstraintName[]> _constraintIndexRebuildRuns = [];
    readonly Dictionary<ConstraintIndexRebuildRunId, EventSequenceNumber> _constraintIndexRebuildStarts = [];
    readonly HashSet<ConstraintIndexRebuildRunId> _finishedConstraintIndexRebuilds = [];
    readonly Queue<ConstraintIndexRebuildRunId> _finishedConstraintIndexRebuildOrder = [];
    bool _rebuildOwnsBoundary;

    /// <inheritdoc/>
    public async Task ConstraintIndexRebuildStarting(ConstraintIndexRebuildRunId runId, IEnumerable<ConstraintName> names)
    {
        if (_finishedConstraintIndexRebuilds.Contains(runId))
        {
            logger.IgnoringFinishedConstraintIndexRebuild(_eventSequenceKey, runId);
            return;
        }
        if (_constraintIndexRebuildRuns.ContainsKey(runId)) return;
        var previous = _constraintIndexRebuildRuns.Keys.FirstOrDefault(active => active.Value == runId.Value);
        if (previous is not null)
        {
            await ConstraintIndexRebuilt(previous, _constraintIndexRebuildRuns[previous], ConstraintIndexRebuildOutcome.ProgressUnknown);
        }
        SynchronizeIndexedDefinitions();
        var rebuilding = names.Distinct().ToArray();
        if (rebuilding.Length == 0) return;
        _constraintIndexRebuildRuns.Add(runId, rebuilding);
        _constraintIndexRebuildStarts.Add(runId, State.SequenceNumber);
        if (_replayScopes.Any(scope => rebuilding.Contains(scope.Definition.Name))) _constraintIndexReplay = null;
        if (rebuilding.Any(_catchUpDefinitionNames.Contains)) ResetRebuildCatchUp();
        foreach (var name in rebuilding)
        {
            _rebuildingConstraintIndexes[name] = _rebuildingConstraintIndexes.GetValueOrDefault(name) + 1;
        }
        UpdateConstraintIndexRepairTimer();
        if (State.ConstraintIndexCatchUpBelow is not null || !rebuilding.Any(IsUniqueConstraint)) return;
        State.ConstraintIndexCatchUpBelow = State.SequenceNumber;
        _rebuildOwnsBoundary = true;
        await PersistCatchUpState(CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task ConstraintIndexRebuilt(ConstraintIndexRebuildRunId runId, IEnumerable<ConstraintName> names, ConstraintIndexRebuildOutcome outcome)
    {
        if (runId.Attempt == Guid.Empty && !_constraintIndexRebuildRuns.ContainsKey(runId))
        {
            var active = _constraintIndexRebuildRuns.Keys.FirstOrDefault(candidate => candidate.Value == runId.Value);
            if (active is null) return;
            runId = active;
        }
        if (!_constraintIndexRebuildRuns.TryGetValue(runId, out var rebuilt)) return;
        foreach (var name in rebuilt)
        {
            if (!_rebuildingConstraintIndexes.TryGetValue(name, out var count)) continue;
            if (count > 1) _rebuildingConstraintIndexes[name] = count - 1;
            else _rebuildingConstraintIndexes.Remove(name);
        }
        if (rebuilt.Any(_catchUpDefinitionNames.Contains)) ResetRebuildCatchUp();
        var unknown = outcome.WroteAnything && outcome.ProcessedThrough is null;
        if (unknown)
        {
            _catchUpDefinitionNames.UnionWith(rebuilt.Where(IsUniqueConstraint));
            var started = _constraintIndexRebuildStarts[runId];
            var from = State.ConstraintIndexCatchUpBelow is { } existing && existing < started ? existing : started;
            State.ConstraintIndexCatchUpBelow = from;
            foreach (var name in rebuilt.Where(_indexedDefinitions.ContainsKey)) MarkConstraintIndexPending(name, from);

            // Progress is not known, so the durable boundary now belongs to catch-up rather than
            // to the job. Catch-up must be allowed to run once the remaining rebuilds end.
            _rebuildOwnsBoundary = false;
        }
        else if (outcome.ProcessedThrough is { } processed)
        {
            foreach (var name in rebuilt.Where(_indexedDefinitions.ContainsKey)) MarkConstraintIndexPending(name, processed.Next());
            UpdateConstraintIndexWatermark();
            try
            {
                await RunConstraintIndexRepairTurn(TimeSpan.FromSeconds(5), false, CancellationToken.None, persist: false);
            }
            catch (Exception error)
            {
                logger.ConstraintIndexRepairFailed(_eventSequenceKey, State.ConstraintIndexWatermark!, State.SequenceNumber, error);
            }
        }

        var boundary = State.ConstraintIndexCatchUpBelow;
        var clear = _rebuildOwnsBoundary && _rebuildingConstraintIndexes.Count == 0 && !unknown;
        if (clear) State.ConstraintIndexCatchUpBelow = null;
        UpdateConstraintIndexWatermark();
        try
        {
            await PersistCatchUpState(CancellationToken.None);
            if (clear) _rebuildOwnsBoundary = false;
            _constraintIndexRebuildRuns.Remove(runId);
            _constraintIndexRebuildStarts.Remove(runId);
            _finishedConstraintIndexRebuilds.Add(runId);
            _finishedConstraintIndexRebuildOrder.Enqueue(runId);
            if (_finishedConstraintIndexRebuildOrder.Count > 256) _finishedConstraintIndexRebuilds.Remove(_finishedConstraintIndexRebuildOrder.Dequeue());
        }
        catch
        {
            if (clear) State.ConstraintIndexCatchUpBelow = boundary;
            foreach (var name in rebuilt) _rebuildingConstraintIndexes[name] = _rebuildingConstraintIndexes.GetValueOrDefault(name) + 1;
            throw;
        }
        finally
        {
            UpdateConstraintIndexRepairTimer();
        }
    }

    bool IsUniqueConstraint(ConstraintName name) => _indexedDefinitions.TryGetValue(name, out var definition) && definition is UniqueConstraintDefinition;

    void ResetRebuildCatchUp()
    {
        _catchUpReplay = null;
        _catchUpExcludedThrough = State.ConstraintIndexCatchUpBelow;
        _catchUpExcluded.Clear();
    }
}
