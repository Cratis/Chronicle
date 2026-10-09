// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_round_tripping_routing.given;

public class a_storage_for_routing : for_EventSequenceStorage.given.an_event_sequence_storage
{
    protected AppendedEvent _readBack;
    protected bool _appendSucceeded;

    protected Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> AppendWithRouting(EventStreamId streamId) => _storage.Append(
        EventSequenceNumber.First,
        "Account",
        "account-1",
        "Payments",
        streamId,
        _eventType,
        CorrelationId.New(),
        [],
        [],
        [],
        DateTimeOffset.UtcNow,
        new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new ExpandoObject() },
        new Dictionary<EventTypeGeneration, EventHash>());

    protected Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>> AppendManyWithRouting(EventStreamId streamId) => _storage.AppendMany(
    [
        new EventToAppendToStorage(EventSequenceNumber.First, "Account", "account-1", "Payments", streamId, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)
    ]);
}
