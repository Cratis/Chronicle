// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency;

public class ValidateConcurrency(IStorage storage) : IValidateConcurrency
{
    /// <inheritdoc/>
    public async Task<ConcurrencyValidationResults> Validate(
        EventSequenceKey eventSequenceKey, EventSourceId eventSourceId, ConcurrencyScope scope)
    {
        var eventSequenceStorage = storage.GetEventStore(eventSequenceKey.EventStore)
            .GetNamespace(eventSequenceKey.Namespace)
            .GetEventSequence(eventSequenceKey.EventSequenceId);

        var tailSequenceNumber = await eventSequenceStorage.GetTailSequenceNumber(
            scope.EventTypes,
            scope.EventSourceId ? eventSourceId : null,
            scope.EventSourceType,
            scope.EventStreamId,
            scope.EventStreamType);

        return tailSequenceNumber <= scope.SequenceNumber
            ? ConcurrencyValidationResults.Success
            : ConcurrencyValidationResults.Failed([new ConcurrencyViolation(scope.SequenceNumber, tailSequenceNumber)]);
    }
}