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
            // A Starting from an older attempt that arrives late must not end the newer run.
            if (runId.Started < previous.Started)
            {
                RememberFinishedConstraintIndexRebuild(runId);
                logger.IgnoringFinishedConstraintIndexRebuild(_eventSequenceKey, runId);
                return;
            }
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
        if (ActiveConstraintIndexRebuild(runId) is not { } activeRun) return;
        runId = activeRun;
        if (!_constraintIndexRebuildRuns.TryGetValue(runId, out var rebuilt))
        {
            // Never started here (or already ended): remember it so a late Starting for it is ignored.
            if (runId.Attempt != Guid.Empty) RememberFinishedConstraintIndexRebuild(runId);
            return;
        }
        ReleaseRebuildingConstraintIndexes(rebuilt);
        if (rebuilt.Any(_catchUpDefinitionNames.Contains)) ResetRebuildCatchUp();
        var unknown = outcome.WroteAnything && outcome.ProcessedThrough is null;
        if (unknown)
        {
            HandRebuildBoundaryToCatchUp(runId, rebuilt);
        }
        else if (outcome.ProcessedThrough is { } processed)
        {
            await RepairAfterRebuild(rebuilt, processed);
        }

        await CompleteConstraintIndexRebuild(runId, rebuilt, unknown);
    }

    ConstraintIndexRebuildRunId? ActiveConstraintIndexRebuild(ConstraintIndexRebuildRunId runId) =>
        runId.Attempt != Guid.Empty || _constraintIndexRebuildRuns.ContainsKey(runId)
            ? runId
            : _constraintIndexRebuildRuns.Keys.FirstOrDefault(candidate => candidate.Value == runId.Value);

    void ReleaseRebuildingConstraintIndexes(ConstraintName[] rebuilt)
    {
        foreach (var name in rebuilt.Where(_rebuildingConstraintIndexes.ContainsKey))
        {
            var count = _rebuildingConstraintIndexes[name];
            if (count > 1) _rebuildingConstraintIndexes[name] = count - 1;
            else _rebuildingConstraintIndexes.Remove(name);
        }
    }

    void HandRebuildBoundaryToCatchUp(ConstraintIndexRebuildRunId runId, ConstraintName[] rebuilt)
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

    async Task RepairAfterRebuild(ConstraintName[] rebuilt, EventSequenceNumber processed)
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

    async Task CompleteConstraintIndexRebuild(ConstraintIndexRebuildRunId runId, ConstraintName[] rebuilt, bool unknown)
    {
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
            RememberFinishedConstraintIndexRebuild(runId);
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

    void RememberFinishedConstraintIndexRebuild(ConstraintIndexRebuildRunId runId)
    {
        if (!_finishedConstraintIndexRebuilds.Add(runId)) return;
        _finishedConstraintIndexRebuildOrder.Enqueue(runId);
        if (_finishedConstraintIndexRebuildOrder.Count > 256) _finishedConstraintIndexRebuilds.Remove(_finishedConstraintIndexRebuildOrder.Dequeue());
    }

    bool IsUniqueConstraint(ConstraintName name) => _indexedDefinitions.TryGetValue(name, out var definition) && definition is UniqueConstraintDefinition;

    void ResetRebuildCatchUp()
    {
        _catchUpReplay = null;
        _catchUpExcludedThrough = State.ConstraintIndexCatchUpBelow;
        _catchUpExcluded.Clear();
    }
}
