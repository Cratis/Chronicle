// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Events.Constraints;

public partial class ConstraintIndexReplay
{
    async Task<bool> MoveNext(IEventCursor cursor, CancellationToken cancellationToken)
    {
        _operationConstraint = null;
        _operationPosition = null;
        return await cursor.MoveNext().WaitAsync(cancellationToken);
    }

    void SettleError(IConstraintDefinition definition, AppendedEvent @event, Exception error)
    {
        var position = @event.Context.SequenceNumber;
        if (_segment != 0 || _retryPass || (_lastSettledError.TryGetValue(definition.Name, out var last) && position <= last)) return;
        _lastSettledError[definition.Name] = position;
        _settledWithError++;
        if (@event.Context.EventType.Id == GlobalEventTypes.Redaction && error is InvalidClosingStreamScope && definition is ClosesStreamConstraintDefinition { EventStreamIdFrom: not null })
        {
            logger.UnrecoverableRedactedScope(definition.Name, position, error);
        }
        else
        {
            logger.UnresolvedReplayEvent(definition.Name, position, error);
        }
    }
}
