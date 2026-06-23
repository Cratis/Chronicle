// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChildCollectionCompliance;

/// <summary>
/// Robustness adjacency for the projection collapse: adding a second child after a child collection with
/// scalar <c>[PII]</c> already exists. The second add re-encrypts the whole snapshot (yielding a collapsed
/// whole-collection difference) at the same time as the <c>$push</c> for the new child. The collapsed
/// <c>$set</c> must be filtered against the child operation so only the <c>$push</c> applies — no Mongo
/// conflict, no duplicated child, both children ciphertext at rest.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_projection_adds_a_second_child_to_a_child_collection_with_pii(MongoDBFixture fixture)
    : given.a_child_collection_compliance_scenario(fixture)
{
    Exception? _error;
    int _storedContactCount;
    bool _bothContactsAreCiphertext;

    protected override string Identifier => "org-1";

    protected override ReadModelObserverType ObserverType => ReadModelObserverType.Projection;

    protected override string SchemaJson =>
        """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "contacts": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "contactId": { "type": "string" },
                  "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] }
                }
              }
            }
          }
        }
        """;

    async Task Because()
    {
        var projection = Substitute.For<IProjection>();
        projection.TargetReadModelSchema.Returns(Schema);
        var step = new EncryptChangeset(Compliance, ObjectComparer, EventStore, EventStoreNamespace);

        await Add(step, projection, EventSequenceNumber.First, new ExpandoObject(), Contact("contact-1", "Jane Doe"));

        var withFirst = Expando(("id", Identifier), ("contacts", new object[] { Contact("contact-1", "Jane Doe") }));
        _error = await Catch.Exception(() => Add(step, projection, new EventSequenceNumber(1), withFirst, Contact("contact-2", "John Roe")));

        var contacts = (await StoredDocument())["contacts"].AsBsonArray;
        _storedContactCount = contacts.Count;
        _bothContactsAreCiphertext = contacts.Count == 2 && contacts.All(c => IsBase64(c.AsBsonDocument["name"].AsString));
    }

    [Fact] void should_add_the_second_child_without_failing() => _error.ShouldBeNull();

    [Fact] void should_keep_exactly_two_children() => _storedContactCount.ShouldEqual(2);

    [Fact] void should_keep_both_children_ciphertext() => _bothContactsAreCiphertext.ShouldBeTrue();

    async Task Add(EncryptChangeset step, IProjection projection, EventSequenceNumber sequenceNumber, ExpandoObject initialState, ExpandoObject child)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(ObjectComparer, Event(sequenceNumber), initialState);
        changeset.AddChild("contacts", child);
        await step.Perform(projection, new ProjectionEventContext(Key, changeset.Incoming, changeset, ProjectionOperationType.None, false));
        await Sink.ApplyChanges(Key, changeset, sequenceNumber);
    }

    AppendedEvent Event(EventSequenceNumber sequenceNumber) =>
        new(
            EventContext.From(EventStore, EventStoreNamespace, EventType.Unknown, EventSourceType.Default, Identifier, EventStreamType.All, EventStreamId.Default, sequenceNumber, CorrelationId.NotSet),
            new ExpandoObject());

    static ExpandoObject Contact(string id, string name) => Expando(("contactId", id), ("name", name));
}
