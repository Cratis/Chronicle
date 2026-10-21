// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.Kernel.Integration.EventRouting.when_round_tripping_routing.given;

public class a_storage_for_routing(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
{
    public AppendedEvent ReadBack = default!;
    public bool AppendSucceeded;

    protected IEventSequenceStorage _storage = default!;
    readonly EventType _eventType = new($"RoutingEvent-{Guid.NewGuid():N}", EventTypeGeneration.First);

    async Task Establish()
    {
        var eventStoreStorage = Services.GetRequiredService<IStorage>().GetEventStore((Concepts.EventStoreName)Constants.EventStore);
        await eventStoreStorage.EventTypes.Register(_eventType, new JsonSchema());
        _storage = eventStoreStorage.GetNamespace($"routing-{Guid.NewGuid():N}").GetEventSequence(EventSequenceId.Log);
    }

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
