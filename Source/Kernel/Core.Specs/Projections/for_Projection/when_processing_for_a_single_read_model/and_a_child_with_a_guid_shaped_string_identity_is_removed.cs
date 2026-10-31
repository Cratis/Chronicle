// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

public class and_a_child_with_a_guid_shaped_string_identity_is_removed : given.a_projection_grain_with_a_children_projection
{
    const string Remaining = "3f2b1234-1234-1234-1234-123456789abc";
    const string RemovedChild = "3f2b5678-5678-5678-5678-123456789abc";

    AppendedEvent[] _events;
    AppendedEvent[] _storedEvents;
    ExpandoObject _result;
    ExpandoObject _storedResult;
    SingleReadModelProjectionState _foldedState;
    SingleReadModelProjectionState _storedFoldedState;

    async Task Establish()
    {
        _events =
        [
            Associated("issue", 1, Remaining, "100"),
            Associated("issue", 2, RemovedChild, "200"),
            Removed("issue", 3, RemovedChild)
        ];

        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
                "comment":{"type":"string"},
                "provider":{"type":"integer"},
                "reference":{"type":"string"}
            }}
            """);
        var removalSchema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"comment":{"type":"string"}}}
            """);
        var eventTypes = Substitute.For<IEventTypesStorage>();
        eventTypes.HasFor(Arg.Any<EventTypeId>(), EventTypeGeneration.First).Returns(true);
        eventTypes.GetFor(_associated.Id, EventTypeGeneration.First).Returns(
            new EventTypeSchema(_associated, EventTypeOwner.Client, EventTypeSource.Code, schema));
        eventTypes.GetFor(_removed.Id, EventTypeGeneration.First).Returns(
            new EventTypeSchema(_removed, EventTypeOwner.Client, EventTypeSource.Code, removalSchema));
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.NotSet);
        var jsonConverter = new Json.ExpandoObjectConverter(new TypeFormats());
        var converter = new EventConverter(EventStore, EventStoreNamespaceName.Default, eventTypes, identities, jsonConverter);
        var storedEvents = new List<AppendedEvent>();
        foreach (var @event in _events)
        {
            var content = jsonConverter.ToJsonObject(@event.Content, @event.Context.EventType == _associated ? schema : removalSchema);
            var bson = BsonDocument.Parse(content.ToJsonString());
            var storedContent = BsonSerializer.Deserialize<BsonDocument>(bson.ToBson());
            var storedEvent = new Event(
                @event.Context.SequenceNumber,
                CorrelationId.NotSet,
                [],
                [IdentityId.NotSet],
                @event.Context.EventType.Id,
                DateTimeOffset.UnixEpoch,
                EventSourceType.Default,
                @event.Context.EventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                [],
                new Dictionary<string, BsonDocument> { ["1"] = storedContent },
                new Dictionary<string, string>(),
                []);
            storedEvents.Add(await converter.ToAppendedEvent(storedEvent));
        }
        _storedEvents = [.. storedEvents];
    }

    async Task Because()
    {
        _result = await Fold(_events);
        _storedResult = await Fold(_storedEvents);
        _foldedState = await _grain.FoldForSingleReadModel(EventStoreNamespaceName.Default, SingleReadModelProjectionState.Empty, _events);
        _storedFoldedState = await _grain.FoldForSingleReadModel(EventStoreNamespaceName.Default, SingleReadModelProjectionState.Empty, _storedEvents);
    }

    [Fact] void should_keep_only_the_remaining_child() => AssociationsOf(_result).Select(_ => _["id"]).ShouldContainOnly(Remaining);
    [Fact] void should_preserve_string_keys_through_mongodb_event_content() => _storedEvents.Select(_ => ((IDictionary<string, object?>)_.Content)["comment"]).ShouldEqual([Remaining, RemovedChild, RemovedChild]);
    [Fact] void should_keep_only_the_remaining_child_after_the_mongodb_round_trip() => AssociationsOf(_storedResult).Select(_ => _["id"]).ShouldContainOnly(Remaining);
    [Fact] void should_keep_only_the_remaining_child_when_folding_from_empty_state() => AssociationsOf(_foldedState.Model).Select(_ => _["id"]).ShouldContainOnly(Remaining);
    [Fact] void should_keep_only_the_remaining_child_when_folding_stored_events_from_empty_state() => AssociationsOf(_storedFoldedState.Model).Select(_ => _["id"]).ShouldContainOnly(Remaining);
}
