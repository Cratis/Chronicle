// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

public partial class ConstraintIndexReplay
{
    bool _readIndividually;

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
            if (position == EventSequenceNumber.Unavailable || position >= endPoint) return (true, count);
            _operationPosition = position;
            if (_scopes.Any(scope => position >= scope.From && !IsPoison(scope.Definition.Name, position)))
            {
                var @event = await events.GetEventAt(position).WaitAsync(cancellationToken);
                if (!await IncludeEvent(@event, cancellationToken)) return (false, count);
            }
            _position = position.Next();
            count++;
        }
        return (false, count);
    }
}
