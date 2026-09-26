// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_a_generation_one_event_has_a_migration : given.an_event_sequence
{
    EventToAppendToStorage _storedEvent;
    ExpandoObject _original;
    ExpandoObject _upcast;
    AppendManyResult _result;

    void Establish()
    {
        _original = new ExpandoObject();
        ((IDictionary<string, object?>)_original)["legacyValue"] = "original";
        _upcast = new ExpandoObject();
        ((IDictionary<string, object?>)_upcast)["value"] = "migrated";
        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Cratis.Chronicle.Schemas.JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(new JsonObject { ["legacyValue"] = "original" });
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<Cratis.Chronicle.Schemas.JsonSchema>()).Returns(_original);
        _eventTypeMigrations.MigrateToAllGenerations(Arg.Any<EventStoreName>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>())
            .Returns(new Dictionary<EventTypeGeneration, ExpandoObject>
            {
                [EventTypeGeneration.First] = _original,
                [new EventTypeGeneration(2)] = _upcast
            });
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call =>
            {
                _storedEvent = call.Arg<IEnumerable<EventToAppendToStorage>>().Single();
                return new[] { new AppendedEvent(EventContext.From(EventStore, EventStoreNamespace, _storedEvent.EventType, _storedEvent.EventSourceType, _storedEvent.EventSourceId, _storedEvent.EventStreamType, _storedEvent.EventStreamId, _storedEvent.SequenceNumber, CorrelationId.NotSet), _original) };
            });
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        [new EventToAppend(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, [], new JsonObject { ["legacyValue"] = "original" })],
        CorrelationId.NotSet,
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_both_generations() => _storedEvent.GenerationalContent.Keys.ShouldContainOnly([EventTypeGeneration.First, new EventTypeGeneration(2)]);
    [Fact] void should_store_the_upcast_content() => _storedEvent.GenerationalContent[new EventTypeGeneration(2)].ShouldEqual(_upcast);
    [Fact] void should_hash_each_generation() => _storedEvent.ContentHashes.Keys.ShouldContainOnly([EventTypeGeneration.First, new EventTypeGeneration(2)]);
}
