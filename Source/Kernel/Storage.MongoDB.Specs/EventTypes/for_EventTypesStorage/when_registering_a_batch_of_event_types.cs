// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

/// <summary>
/// Registering a batch collapses to one read and one bulk write, and reports back only the event types whose
/// stored document actually changed - that set is what drives cache invalidation and the system events the
/// service appends, so reporting a type that did not change is as wrong as missing one that did.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_registering_a_batch_of_event_types(MongoDBFixture fixture) : given.an_event_types_storage(fixture)
{
    const string FirstSchema = /*lang=json,strict*/ "{\"type\":\"object\",\"properties\":{\"something\":{\"type\":\"string\"}}}";
    const string ChangedSchema = /*lang=json,strict*/ "{\"type\":\"object\",\"properties\":{\"somethingElse\":{\"type\":\"string\"}}}";

    static readonly EventTypeId _first = "the-first-event-type";
    static readonly EventTypeId _second = "the-second-event-type";

    IEnumerable<EventTypeId> _firstRegistration;
    IEnumerable<EventTypeId> _registeringTheSameAgain;
    IEnumerable<EventTypeId> _registeringWithOneChanged;
    long _storedCount;

    async Task Because()
    {
        _firstRegistration = await _storage.Register([await ToRegister(_first, FirstSchema), await ToRegister(_second, FirstSchema)]);
        _registeringTheSameAgain = await _storage.Register([await ToRegister(_first, FirstSchema), await ToRegister(_second, FirstSchema)]);
        _registeringWithOneChanged = await _storage.Register([await ToRegister(_first, FirstSchema), await ToRegister(_second, ChangedSchema)]);
        _storedCount = await _storedDocuments.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    [Fact] void should_report_every_event_type_as_changed_the_first_time() => _firstRegistration.ShouldContainOnly(_first, _second);
    [Fact] void should_report_nothing_as_changed_when_registering_the_same_again() => _registeringTheSameAgain.ShouldBeEmpty();
    [Fact] void should_report_only_the_event_type_that_changed() => _registeringWithOneChanged.ShouldContainOnly(_second);
    [Fact] void should_store_one_document_per_event_type() => _storedCount.ShouldEqual(2L);

    static async Task<EventTypeToRegister> ToRegister(EventTypeId id, string schemaJson) => new(
        new EventTypeDefinition(
            id,
            EventTypeOwner.Client,
            false,
            [new EventTypeGenerationDefinition(EventTypeGeneration.First, await JsonSchema.FromJsonAsync(schemaJson))],
            []),
        EventTypeSource.Code);
}
