// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    async Task RepairConstraintIndexesBeforeAppend()
    {
        var now = ConstraintIndexTimeProvider.GetUtcNow();
        if (now < _nextConstraintRepairAttempt || (_lastAppendRepairEnded is { } ended && now - ended < TimeSpan.FromSeconds(5))) return;
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            _constraintIndexReplay ??= CreateConstraintIndexReplay();
            var result = await _constraintIndexReplay.Continue(int.MaxValue, budget.Token);
            foreach (var name in result.Abandoned) _abandonedConstraintIndexes.Add(name);
            foreach (var (name, failure) in result.Failed)
            {
                logger.ConstraintIndexUpdateOutcome(_eventSequenceKey, name, failure.Position ?? State.ConstraintIndexWatermark!, State.ConstraintIndexWatermark!, State.SequenceNumber, ConstraintIndexUpdateOutcome.Failed, failure.Error);
            }
            if (_constraintIndexReplay.RangeReadFailed)
            {
                MarkConstraintIndexGap(State.ConstraintIndexWatermark!);
                _constraintIndexReplay = null;
            }
            if (result.Failed.Count > 0 || result.Abandoned.Any())
            {
                _constraintRepairFailures++;
                _nextConstraintRepairAttempt = ConstraintIndexTimeProvider.GetUtcNow().AddSeconds(Math.Min(60, Math.Pow(2, Math.Min(6, _constraintRepairFailures - 1))));
            }
            if (!result.IsCompleted) return;
            foreach (var scope in _replayScopes.Where(scope => !_abandonedConstraintIndexes.Contains(scope.Definition.Name)))
            {
                if (_replayEndPoint < State.SequenceNumber)
                {
                    _pendingConstraintIndexes[scope.Definition.Name] = _replayEndPoint;
                }
                else
                {
                    _pendingConstraintIndexes.Remove(scope.Definition.Name);
                }
            }
            _constraintIndexReplay = null;
            _constraintRepairFailures = 0;
            _nextConstraintRepairAttempt = default;
            UpdateConstraintIndexWatermark();
            await WriteStateAsync();
            _metrics?.ConstraintIndexRepaired();
        }
        catch (Exception error)
        {
            // The range could not be read or its progress could not be persisted. Do not admit on unknown state.
            MarkConstraintIndexGap(State.ConstraintIndexWatermark!);
            _constraintIndexReplay = null;
            _constraintRepairFailures++;
            _nextConstraintRepairAttempt = ConstraintIndexTimeProvider.GetUtcNow().AddSeconds(Math.Min(60, Math.Pow(2, Math.Min(6, _constraintRepairFailures - 1))));
            logger.ConstraintIndexRepairFailed(_eventSequenceKey, State.ConstraintIndexWatermark!, State.SequenceNumber, error);
        }
        finally
        {
            _lastAppendRepairEnded = ConstraintIndexTimeProvider.GetUtcNow();
        }
    }

    ConstraintIndexReplay CreateConstraintIndexReplay()
    {
        _replayScopes = _pendingConstraintIndexes.Select(pair => new ConstraintIndexReplayScope(_indexedDefinitions[pair.Key], pair.Value)).ToArray();
        _replayEndPoint = State.SequenceNumber;
        var namespaceStorage = storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace);
        return new(
            EventSequenceStorage,
            namespaceStorage.GetUniqueConstraintsStorage(_eventSequenceId),
            ClosedStreamsStorage,
            new StoredConstraintEventContent(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, EventTypesStorage, jsonComplianceManagerProvider, expandoObjectConverter),
            new ConstraintReplayLogger(logger),
            _replayScopes,
            _replayEndPoint);
    }

    sealed class ConstraintReplayLogger(ILogger<EventSequence> target) : ILogger<ConstraintIndexReplay>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => target.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => target.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => target.Log(logLevel, eventId, state, exception, formatter);
    }
}
