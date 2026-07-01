// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChildCollectionCompliance;

/// <summary>
/// Regression for removing a <c>[ChildrenFrom]</c> child that carries a <c>[PII]</c> member from the real
/// MongoDB sink. The child is stored with its <c>[PII]</c> member encrypted at rest, so a <c>$pull</c> that
/// matches the whole plaintext removal document never matches the encrypted stored element and the child is
/// never removed (the in-memory sink removes it by key, so the two sinks silently diverge). Two contacts are
/// seeded encrypted, then one is removed by its identifier through the projection child-removal path.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_removing_a_child_carrying_pii_from_a_projection(MongoDBFixture fixture)
    : given.a_child_collection_compliance_scenario(fixture)
{
    int _storedCountBefore;
    int _storedCountAfter;
    string _remainingContactId = string.Empty;

    protected override string Identifier => "contact-owner-1";

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
                  "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] },
                  "status": { "type": "string" }
                }
              }
            }
          }
        }
        """;

    async Task Because()
    {
        // Seed two contacts, each with a [PII] name, encrypted at rest as the whole stored state.
        var pipeline = new ReducerPipeline(ReadModel, Sink, ObjectComparer, Compliance, EventStore, EventStoreNamespace);
        await pipeline.Handle(
            new ReducerContext([Event()], Key),
            (_, _) => Task.FromResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), TwoContacts())));

        _storedCountBefore = (await StoredDocument())["contacts"].AsBsonArray.Count;

        // Remove one contact by its identifier through the projection child-removal path. The stored name is
        // ciphertext, so a whole-document $pull against the plaintext removal state would miss.
        var removeChangeset = new Changeset<AppendedEvent, ExpandoObject>(ObjectComparer, Event(), TwoContacts());
        removeChangeset.RemoveChild("contacts", "contactId", "contact-1", ArrayIndexers.NoIndexers);
        await Sink.ApplyChanges(Key, removeChangeset, new EventSequenceNumber(1));

        var after = (await StoredDocument())["contacts"].AsBsonArray;
        _storedCountAfter = after.Count;
        _remainingContactId = after.Count > 0 ? after[0].AsBsonDocument["contactId"].AsString : string.Empty;
    }

    [Fact] void should_have_seeded_two_contacts() => _storedCountBefore.ShouldEqual(2);

    [Fact] void should_remove_the_pii_child() => _storedCountAfter.ShouldEqual(1);

    [Fact] void should_keep_the_other_child() => _remainingContactId.ShouldEqual("contact-2");

    static ExpandoObject TwoContacts() =>
        Expando(("id", "contact-owner-1"), ("contacts", new object[] { Contact("contact-1", "Jane Doe"), Contact("contact-2", "John Roe") }));

    static ExpandoObject Contact(string contactId, string name) =>
        Expando(("contactId", contactId), ("name", name), ("status", "active"));

    AppendedEvent Event() =>
        new(
            EventContext.From(EventStore, EventStoreNamespace, EventType.Unknown, EventSourceType.Default, Identifier, EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet),
            new ExpandoObject());
}
