// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;

using Phase = Cratis.Chronicle.Events.Constraints.ConstraintIndexReplayPhase;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Folds a fixed stored range into final key states before repairing differences. Progress survives bounded turns.
/// </summary>
/// <param name="events">The ordered event sequence.</param>
/// <param name="unique">The settled unique-constraint store.</param>
/// <param name="closed">The owned closure store.</param>
/// <param name="content">The stored-event plaintext reader.</param>
/// <param name="logger">The replay logger.</param>
/// <param name="scopes">The pending ranges and closing coverage starts.</param>
/// <param name="endPoint">The exclusive fixed end point.</param>
/// <param name="keyLimit">The combined staged-key and difference cap.</param>
public partial class ConstraintIndexReplay(
    IEventSequenceStorage events,
    IUniqueConstraintsStorage unique,
    IClosedStreamsConstraintStorage closed,
    StoredConstraintEventContent content,
    ILogger<ConstraintIndexReplay> logger,
    IEnumerable<ConstraintIndexReplayScope> scopes,
    EventSequenceNumber endPoint,
    int keyLimit = 100000)
{
    readonly ConstraintIndexReplayScope[] _scopes = scopes.ToArray();
    readonly Dictionary<UniqueKey, UniqueState> _unique = [];
    readonly Dictionary<ClosingKey, ClosingState> _closed = [];
    readonly List<ConstraintIndexReplayDifference> _differences = [];
    readonly Dictionary<ConstraintName, ConstraintIndexReplayFailure> _failed = [];
    readonly HashSet<ConstraintName> _abandoned = [];
    readonly Dictionary<ConstraintName, EventSequenceNumber> _lastSettledError = [];
    ConstraintName? _operationConstraint;
    EventSequenceNumber? _operationPosition;
    EventSequenceNumber? _position;
    int _segments = 1;
    int _segment;
    int _applied;
    int _settled;
    int _settledWithError;
    int _maxKeysHeld;
    Phase _phase;
    bool _needsRetryPass;
    bool _retryPass;
    bool _overflow;

    /// <summary>
    /// Gets the number of key-hash segments used by this fold.
    /// </summary>
    internal int SegmentCount => _segments;

    /// <summary>
    /// Gets the highest combined count of staged keys and collected differences.
    /// </summary>
    internal int MaxKeysHeld => _maxKeysHeld;

    /// <summary>
    /// Continues the fold or its two-phase writes without writing unfinished staged state.
    /// </summary>
    /// <param name="eventLimit">The maximum relevant events processed in this turn, across segment passes.</param>
    /// <param name="cancellationToken">The enclosing turn's deadline.</param>
    /// <returns>The accumulated progress and this turn's failed constraints.</returns>
    public async Task<ConstraintIndexReplayResult> Continue(int eventLimit = 1000, CancellationToken cancellationToken = default)
    {
        _failed.Clear();
        _abandoned.Clear();
        _operationConstraint = null;
        _operationPosition = null;
        if (_scopes.Length == 0) _phase = Phase.Complete;
        var processed = 0;
        try
        {
            while (_phase != Phase.Complete && !cancellationToken.IsCancellationRequested)
            {
                if (_phase == Phase.Reading)
                {
                    if (processed >= eventLimit) break;
                    var (finished, count) = await ReadSegment(eventLimit - processed, cancellationToken);
                    processed += count;
                    if (Interrupted()) break;
                    if (!finished) continue;
                    _phase = Phase.Collecting;
                }
                if (_phase == Phase.Collecting)
                {
                    await CollectDifferences(cancellationToken);
                    _segment++;
                    if (_segment < _segments && _differences.Count < Math.Max(1, keyLimit / 2))
                    {
                        BeginSegment();
                        continue;
                    }
                    _phase = Phase.Releasing;
                }
                if (_phase == Phase.Releasing)
                {
                    await ReleaseDifferences(cancellationToken);
                    if (Interrupted()) break;
                    _phase = Phase.Claiming;
                }
                if (_phase == Phase.Claiming)
                {
                    await ClaimDifferences(cancellationToken);
                    if (Interrupted()) break;
                    _differences.Clear();
                    if (_segment < _segments)
                    {
                        BeginSegment();
                    }
                    else if (_needsRetryPass && !_retryPass)
                    {
                        // Re-read instead of retaining an unbounded list of refused claims from flushed segments.
                        // All releases have now completed; already-written final keys produce no difference.
                        _retryPass = true;
                        _segment = 0;
                        BeginSegment();
                    }
                    else
                    {
                        _phase = Phase.Complete;
                    }
                }
            }
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // Keep staged state and already-confirmed writes for the next turn, never report completion.
        }
        catch (Exception error)
        {
            foreach (var constraint in _operationConstraint is { } name ? [name] : _scopes.Select(scope => scope.Definition.Name).Distinct()) Fail(constraint, _operationPosition, error);
        }

        return new(_phase == Phase.Complete, _applied, _settled, _settledWithError, new Dictionary<ConstraintName, ConstraintIndexReplayFailure>(_failed), _abandoned.ToArray());
    }

    bool Interrupted() => _failed.Count > 0 || _abandoned.Count > 0;

    void Fail(ConstraintName name, EventSequenceNumber? position, Exception error)
    {
        _failed.TryAdd(name, new(position, error));
        logger.FailedReplayRead(name, position, error);
    }

    void BeginSegment()
    {
        ResetCollection();
        _position = null;
        _phase = Phase.Reading;
    }

    bool Fits() => !_overflow;

    void TrackKeys() => _maxKeysHeld = Math.Max(_maxKeysHeld, _unique.Count + _closed.Count + _differences.Count);

    void Split()
    {
        _segments = checked(_segments * 2);
        _overflow = false;
        _segment = 0;
        _unique.Clear();
        _closed.Clear();
        _differences.Clear();
        BeginSegment();
    }
}
