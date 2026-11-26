// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_OutboxEventTypeQueryFilter.given;

public class a_mixed_catalog : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _eventStore;
    protected EventSequenceQueryCriteria _criteria = EventSequenceQueryCriteria.Empty;
    protected EventSequenceQueryCriteria? _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStore = _storage.GetEventStore((EventStoreName)"store");
        _eventStore.EventTypes.GetLatestForAllEventTypes().Returns(
        [
            Registration("public-one", EventTypeVisibility.Public),
            Registration("public-two", EventTypeVisibility.Public),
            Registration("private", EventTypeVisibility.Private),
            Registration("unspecified", EventTypeVisibility.Unspecified)
        ]);
    }

    protected static EventTypeSchema Registration(string id, EventTypeVisibility visibility) =>
        new(new Concepts.Events.EventType(id, EventTypeGeneration.First), EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromJson("{\"type\":\"object\"}"), visibility);

    protected static string[] Ids(EventSequenceQueryCriteria? criteria) => [.. criteria!.EventTypes!.Select(_ => _.Id.Value)];

    protected async Task Narrow(string sequence) => _result = await OutboxEventTypeQueryFilter.Narrow(_storage, "store", sequence, _criteria);
}
