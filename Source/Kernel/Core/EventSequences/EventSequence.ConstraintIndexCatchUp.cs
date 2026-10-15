// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    readonly ConstraintIndexCatchUpKeys _catchUpExcluded = new();
    EventSequenceNumber? _catchUpExcludedThrough;
    ConstraintIndexReplay? _catchUpReplay;
    bool _catchUpBoundaryDirty;
    bool _catchUpTracking;

    /// <summary>
    /// Gets or sets the exclusion cap, lowered only by deterministic specifications.
    /// </summary>
    internal int ConstraintIndexCatchUpKeyLimit { get; set; } = 100000;

    /// <summary>
    /// Gets the retained exclusion count for memory-bound specifications.
    /// </summary>
    internal int ConstraintIndexCatchUpExcludedKeyCount => _catchUpExcluded.Count;

    async Task ExcludeLiveConstraintKeys(ConstraintValidationContext context, EventSequenceNumber number)
    {
        if (!_catchUpTracking || State.ConstraintIndexCatchUpBelow is not { } boundary || number < boundary) return;
        foreach (var definition in _indexedDefinitions.Values.OfType<UniqueConstraintDefinition>())
        {
            _catchUpExcluded.Include(definition, context);
            if (_catchUpExcluded.Count < ConstraintIndexCatchUpKeyLimit) continue;
            await RaiseConstraintCatchUpBoundary();
            return;
        }
    }

    async Task RaiseConstraintCatchUpBoundary()
    {
        State.ConstraintIndexCatchUpBelow = State.SequenceNumber;
        _catchUpBoundaryDirty = true;

        // No await separates invalidation from the new in-memory boundary: a failed snapshot
        // cannot leave differences computed at the old boundary eligible for writing.
        _catchUpReplay?.RaiseCatchUpBoundary(State.SequenceNumber);
        _catchUpExcluded.Clear();
        _catchUpExcludedThrough = State.SequenceNumber;
        await PersistCatchUpState(CancellationToken.None);
        _catchUpBoundaryDirty = false;
    }

    async Task RunConstraintIndexCatchUpTurn(CancellationToken cancellationToken)
    {
        var slots = ConstraintIndexCatchUpSlots.For(storage);
        if (!slots.TryEnter(this)) return;
        _catchUpTracking = true;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(1750), ConstraintIndexTimeProvider);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        try
        {
            if (_catchUpBoundaryDirty)
            {
                await PersistCatchUpState(budget.Token);
                _catchUpBoundaryDirty = false;
            }
            var count = await RebuildCatchUpExclusions(budget.Token);
            if (_catchUpExcludedThrough! < State.SequenceNumber || count >= 1000) return;
            _catchUpReplay ??= CreateCatchUpReplay();
            var result = await _catchUpReplay.Continue(1000 - count, budget.Token);
            if (CatchUpTurnFailed(result)) return;
            if (!result.IsCompleted) return;
            await CompleteConstraintCatchUp(budget.Token);
        }
        catch (OperationCanceledException) when (budget.IsCancellationRequested)
        {
            // Reading progress is retained. A started group has independent per-write deadlines;
            // its persisted marker remains set if the write or its snapshot did not complete.
        }
        catch (Exception error)
        {
            BackOffConstraintRepair();
            logger.ConstraintIndexRepairFailed(_eventSequenceKey, State.ConstraintIndexWatermark!, State.SequenceNumber, error);
        }
        finally
        {
            slots.LeaveTurn(this, State.ConstraintIndexCatchUpRefusing?.Any() == true);
        }
    }

    async Task<int> RebuildCatchUpExclusions(CancellationToken cancellationToken)
    {
        _catchUpExcludedThrough ??= State.ConstraintIndexCatchUpBelow;
        if (_catchUpExcludedThrough! >= State.SequenceNumber) return 0;
        var definitions = _indexedDefinitions.Values.OfType<UniqueConstraintDefinition>().ToArray();
        var types = definitions.SelectMany(definition => definition.EventDefinitions.Select(item => item.EventTypeId).Concat(definition.RemovedWith)).Distinct().Select(id => new EventType(id, EventTypeGeneration.First)).ToArray();
        if (types.Length == 0)
        {
            _catchUpExcludedThrough = State.SequenceNumber;
            return 0;
        }
        var content = CatchUpContent();
        using var cursor = await EventSequenceStorage.GetFromSequenceNumber(_catchUpExcludedThrough!, eventTypes: types, cancellationToken: cancellationToken).WaitAsync(cancellationToken);
        var count = 0;
        while (await cursor.MoveNext().WaitAsync(cancellationToken))
        {
            foreach (var @event in cursor.Current)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (@event.Context.SequenceNumber >= State.SequenceNumber) break;
                if (count >= 1000) return count;
                var plaintext = await content.Release(@event, cancellationToken);
                var context = new ConstraintValidationContext([], @event.Context.EventSourceId, @event.Context.EventType.Id, plaintext, @event.Context.EventSourceType, @event.Context.EventStreamType, @event.Context.EventStreamId);
                _catchUpExcludedThrough = @event.Context.SequenceNumber.Next();
                count++;
                foreach (var definition in definitions)
                {
                    _catchUpExcluded.Include(definition, context);
                    if (_catchUpExcluded.Count < ConstraintIndexCatchUpKeyLimit) continue;
                    await RaiseConstraintCatchUpBoundary();
                    return count;
                }
            }
        }
        _catchUpExcludedThrough = State.SequenceNumber;
        return count;
    }

    ConstraintIndexReplay CreateCatchUpReplay()
    {
        var namespaceStorage = storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace);
        _catchUpSettledErrors = 0;
        return new(
            EventSequenceStorage,
            namespaceStorage.GetUniqueConstraintsStorage(_eventSequenceId),
            ClosedStreamsStorage,
            CatchUpContent(),
            constraintIndexReplayLogger ?? NullLogger<ConstraintIndexReplay>.Instance,
            _indexedDefinitions.Values.OfType<UniqueConstraintDefinition>().Select(definition => new ConstraintIndexReplayScope(definition, EventSequenceNumber.First)),
            State.ConstraintIndexCatchUpBelow!)
        {
            ConstraintIndexTimeProvider = ConstraintIndexTimeProvider,
            StorageErrorClassifier = _storageErrorClassifier,
            SettledPoison = _catchUpPoisonTracker.Settled,
            CatchUp = new(
                difference => difference.Source is { } source && _catchUpExcluded.Contains(difference.Name, difference.ScopeKey, source, difference.Retained),
                _pendingConstraintIndexes.ContainsKey,
                PersistCatchUpRefusal,
                ClearCatchUpRefusal)
            {
                UnderRefusal = State.ConstraintIndexCatchUpRefusing?.Any() == true
            }
        };
    }

    StoredConstraintEventContent CatchUpContent() => new(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, EventTypesStorage, jsonComplianceManagerProvider, expandoObjectConverter);
}
