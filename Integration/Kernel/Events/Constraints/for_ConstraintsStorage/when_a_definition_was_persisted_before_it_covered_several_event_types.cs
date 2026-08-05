// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using ConceptsEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using context = Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_ConstraintsStorage.when_a_definition_was_persisted_before_it_covered_several_event_types.context;

namespace Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_ConstraintsStorage;

/// <summary>
/// A store written before a unique event type constraint could cover several event types holds the covered event
/// type as a single scalar element. A kernel that reads that document without mapping it gets a definition whose
/// covered event types are absent, and constraint registration compares every stored definition with the incoming
/// one - so one such document takes registration down for the whole event store and the client never finishes
/// connecting.
/// <para>
/// The mapping is a serializer, and a serializer only runs if something puts it on the read path. That is the part
/// no unit-level spec can settle: a spec that constructs the serializer and calls it exercises a unit the live path
/// never reaches. This boots a real silo over a real store and reads the planted document back through the storage
/// the kernel actually uses, so it fails whenever the serializer is registered in a way that does not take.
/// </para>
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class when_a_definition_was_persisted_before_it_covered_several_event_types(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public IConstraintDefinition Read = default!;
        public Exception? ComparisonError;
        public bool ComparesEqualToTheIncomingDefinition;

        ConstraintName _name = default!;
        EventTypeId _eventTypeId = default!;
        IMongoCollection<BsonDocument> _collection = default!;

        async Task Establish()
        {
            _name = new ConstraintName($"legacy-constraint-{Guid.NewGuid():N}");
            _eventTypeId = new EventTypeId($"the-event-type-{Guid.NewGuid():N}");

            var database = Services.GetRequiredService<IDatabase>();
            _collection = database
                .GetEventStoreDatabase((ConceptsEventStoreName)Constants.EventStore)
                .GetCollection<BsonDocument>(WellKnownCollectionNames.Constraints);

            // The shape a pre-16.12 kernel wrote: the concrete type named by the driver's discriminator, and the
            // covered event type as a single scalar element rather than the sequence the current record declares.
            await _collection.InsertOneAsync(new BsonDocument
            {
                { "_id", $"{_name}-v1" },
                { "name", _name.Value },
                { "version", 1L },
                {
                    "definition", new BsonDocument
                    {
                        { "_t", nameof(UniqueEventTypeConstraintDefinition) },
                        { "_id", _name.Value },
                        { "eventTypeId", _eventTypeId.Value }
                    }
                }
            });
        }

        async Task Because()
        {
            var storage = Services.GetRequiredService<IStorage>();
            var definitions = await storage.GetEventStore((ConceptsEventStoreName)Constants.EventStore).Constraints.GetDefinitions();
            Read = definitions.First(_ => _.Name == _name);

            ComparisonError = Catch.Exception(() =>
                ComparesEqualToTheIncomingDefinition = Read.Equals(new UniqueEventTypeConstraintDefinition(_name, [_eventTypeId])));
        }

        public EventTypeId ExpectedEventTypeId => _eventTypeId;

        async Task Destroy() => await _collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", $"{_name}-v1"));
    }

    [Fact] void should_read_it_as_a_unique_event_type_constraint() => Context.Read.ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_cover_the_single_event_type_it_was_persisted_with() => ((UniqueEventTypeConstraintDefinition)Context.Read).EventTypeIds.ShouldContainOnly([Context.ExpectedEventTypeId]);
    [Fact] void should_compare_with_the_incoming_definition_rather_than_throw() => Context.ComparisonError.ShouldBeNull();
    [Fact] void should_compare_equal_to_the_incoming_definition() => Context.ComparesEqualToTheIncomingDefinition.ShouldBeTrue();
}
