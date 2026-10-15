// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

public partial class ConstraintIndexReplay
{
    /// <summary>
    /// Bounds point-read fallback when a page fault has not yet been attributed to a slot.
    /// </summary>
    internal const int IndividualReadWindowSize = 1000;
    bool _readIndividually;
    int _individualReadsRemaining;
    EventSequenceNumber? _individualReadFailureEnd;
    EventSequenceNumber? _knownBadReadEnd;

    EventType[] ReplayEventTypes() => _scopes.SelectMany(scope => scope.Definition switch
    {
        UniqueConstraintDefinition unique => unique.EventDefinitions.Select(item => item.EventTypeId).Concat(unique.RemovedWith),
        ClosesStreamConstraintDefinition closing => closing.EventTypeIds.Concat(closing.ReopenedBy).Append(GlobalEventTypes.Redaction),
        _ => []
    }).Distinct().Select(id => new EventType(id, EventTypeGeneration.First)).ToArray();

    async Task<(bool Finished, int Count)> ReadIndividualEvents(EventType[] types, int limit, CancellationToken cancellationToken)
    {
        var count = 0;
        while (count < limit)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _operationConstraint = null;
            _operationPosition = null;
            var position = await events.GetNextSequenceNumberGreaterOrEqualThan(_position!, types).WaitAsync(cancellationToken);
            StorageCallSucceeded = true;
            if (position == EventSequenceNumber.Unavailable || position >= _endPoint) return (true, count);
            _operationPosition = position;
            if (_scopes.Any(scope => position >= scope.From && !IsPoison(scope.Definition.Name, position)))
            {
                var @event = await ReadIndividualEvent(position, cancellationToken);
                if (!await IncludeEvent(@event, cancellationToken)) return (false, count);
            }
            _position = position.Next();
            _operationPosition = null;
            count++;
            var through = _readIndividually ? _individualReadFailureEnd : _knownBadReadEnd;
            if ((through is { } end && _position! >= end) || (_readIndividually && --_individualReadsRemaining <= 0))
            {
                _readIndividually = false;
                _individualReadFailureEnd = null;
                return (false, count);
            }
        }
        return (false, count);
    }

    async Task<AppendedEvent> ReadIndividualEvent(EventSequenceNumber position, CancellationToken cancellationToken)
    {
        try
        {
            return await events.GetEventAt(position).WaitAsync(cancellationToken);
        }
        catch (Exception error) when (!cancellationToken.IsCancellationRequested && StorageErrorClassifier?.IsTransient(error) is false)
        {
            var end = position.Next();
            _individualReadFailureEnd = end;
            if (_knownBadReadEnd is not { } known || end > known) _knownBadReadEnd = end;
            throw;
        }
    }
}
