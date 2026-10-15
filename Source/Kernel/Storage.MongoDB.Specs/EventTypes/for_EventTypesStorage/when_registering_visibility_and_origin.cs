// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

/// <summary>
/// Visibility and origin are written with the event type, read back from the stored document, never undone by a
/// registration that says nothing, and absent from a document stored before they existed.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_registering_visibility_and_origin(MongoDBFixture fixture) : given.an_event_types_storage(fixture)
{
    static readonly EventTypeId _single = "registered-singly";
    static readonly EventTypeId _batched = "registered-in-a-batch";
    static readonly EventTypeId _legacy = "stored-before-visibility";

    bool _firstChanged;
    bool _sameChanged;
    bool _visibilityChanged;
    IEnumerable<EventTypeId> _batchSame;
    EventTypeSchema _singleAfterUnspecified;
    EventTypeSchema _singleAfterPrivate;
    EventTypeSchema _batchedStored;
    EventTypeSchema _batchedAfterUnspecified;
    EventTypeSchema _legacyRead;
    BsonDocument _legacyDocument;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("{\"type\":\"object\"}");
        var type = new EventType(_single, EventTypeGeneration.First);
        _firstChanged = await _storage.Register(type, schema, EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Public, "owning-store");
        _sameChanged = await _storage.Register(type, schema, EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Public, "owning-store");
        await _storage.Register(type, schema, EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Unspecified, "owning-store");
        _singleAfterUnspecified = await _storage.GetFor(_single);
        _visibilityChanged = await _storage.Register(type, schema, EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Private, "owning-store");
        _singleAfterPrivate = await _storage.GetFor(_single);

        await _storage.Register([ToRegister(_batched, schema, EventTypeVisibility.Public, "owning-store")]);
        _batchedStored = await _storage.GetFor(_batched);
        _batchSame = await _storage.Register([ToRegister(_batched, schema, EventTypeVisibility.Public, "owning-store")]);
        await _storage.Register([ToRegister(_batched, schema, EventTypeVisibility.Unspecified, "owning-store")]);
        _batchedAfterUnspecified = await _storage.GetFor(_batched);

        await _storedDocuments.InsertOneAsync(BsonDocument.Parse(
            "{ \"_id\": \"stored-before-visibility\", \"owner\": 1, \"source\": 1, \"tombstone\": false, \"schemas\": { \"1\": { \"type\": \"object\" } } }"));
        _legacyRead = await _storage.GetFor(_legacy);
        _legacyDocument = await _storedDocuments.Find(new BsonDocument("_id", "registered-singly")).FirstAsync();
    }

    [Fact] void should_report_the_first_registration_as_a_change() => _firstChanged.ShouldBeTrue();
    [Fact] void should_report_the_same_registration_as_no_change() => _sameChanged.ShouldBeFalse();
    [Fact] void should_not_undo_the_visibility_when_a_client_sends_nothing() => _singleAfterUnspecified.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_keep_the_origin() => _singleAfterUnspecified.Origin.ShouldEqual("owning-store");
    [Fact] void should_report_a_visibility_change() => _visibilityChanged.ShouldBeTrue();
    [Fact] void should_persist_the_changed_visibility() => _singleAfterPrivate.Visibility.ShouldEqual(EventTypeVisibility.Private);
    [Fact] void should_persist_visibility_registered_in_a_batch() => _batchedStored.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_persist_origin_registered_in_a_batch() => _batchedStored.Origin.ShouldEqual("owning-store");
    [Fact] void should_report_the_same_batch_as_no_change() => _batchSame.ShouldBeEmpty();
    [Fact] void should_not_undo_batched_visibility_when_a_client_sends_nothing() => _batchedAfterUnspecified.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_read_a_document_stored_before_visibility_existed_as_unspecified() => _legacyRead.Visibility.ShouldEqual(EventTypeVisibility.Unspecified);
    [Fact] void should_read_a_document_stored_before_visibility_existed_with_no_origin() => _legacyRead.Origin.ShouldEqual(string.Empty);
    [Fact] void should_write_visibility_into_the_document() => _legacyDocument.Contains("visibility").ShouldBeTrue();

    static EventTypeToRegister ToRegister(EventTypeId id, JsonSchema schema, EventTypeVisibility visibility, string origin) => new(
        new EventTypeDefinition(id, EventTypeOwner.Client, false, [new EventTypeGenerationDefinition(EventTypeGeneration.First, schema)], []),
        EventTypeSource.Code,
        visibility,
        origin);
}
