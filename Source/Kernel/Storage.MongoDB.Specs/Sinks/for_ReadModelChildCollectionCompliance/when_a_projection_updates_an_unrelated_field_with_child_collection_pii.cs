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
/// Regression for the projection sink UPDATE leg of child-collection PII. After a child carrying scalar
/// <c>[PII]</c> has been added, a later event that only changes an unrelated top-level field re-encrypts
/// the whole decrypted state again. That re-encryption yields a nested child-PII difference with no array
/// indexers (<c>contacts.name</c>) which — without the collapse the reducer pipeline already performs —
/// reaches the sink as a non-positional dotted <c>$set</c> and is rejected with WriteError Code 28.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_projection_updates_an_unrelated_field_with_child_collection_pii(MongoDBFixture fixture)
    : given.a_child_collection_compliance_scenario(fixture)
{
    const string PlaintextName = "Jane Doe";
    const string PlaintextEmail = "jane@example.com";

    Exception? _error;
    int _storedContactCount;
    string _storedStatus = string.Empty;
    bool _storedNameIsCiphertext;

    protected override string Identifier => "org-1";

    protected override ReadModelObserverType ObserverType => ReadModelObserverType.Projection;

    protected override string SchemaJson =>
        """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "status": { "type": "string" },
            "contacts": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "contactId": { "type": "string" },
                  "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] },
                  "contactEmail": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] }
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

        // Insert leg ($push) — add a child carrying scalar [PII], encrypting it at rest.
        var insert = new Changeset<AppendedEvent, ExpandoObject>(ObjectComparer, Event(EventSequenceNumber.First), new ExpandoObject());
        insert.AddChild("contacts", Contact());
        await step.Perform(projection, new ProjectionEventContext(Key, insert.Incoming, insert, ProjectionOperationType.None, false));
        await Sink.ApplyChanges(Key, insert, EventSequenceNumber.First);

        // Update leg — a later event changes only an unrelated top-level field. The pipeline hands
        // EncryptChangeset the decrypted (plaintext) state, so re-encryption of the existing child
        // produces a nested child-PII difference that must not reach the sink as a dotted $set.
        var decryptedInitialState = Expando(("id", Identifier), ("contacts", new object[] { Contact() }));
        var update = new Changeset<AppendedEvent, ExpandoObject>(ObjectComparer, Event(new EventSequenceNumber(1)), decryptedInitialState);
        update.Add(new PropertiesChanged<ExpandoObject>(null!, [new PropertyDifference("status", null, "approved")]));
        await step.Perform(projection, new ProjectionEventContext(Key, update.Incoming, update, ProjectionOperationType.None, false));

        _error = await Catch.Exception(() => Sink.ApplyChanges(Key, update, new EventSequenceNumber(1)));

        var stored = await StoredDocument();
        var contacts = stored["contacts"].AsBsonArray;
        _storedContactCount = contacts.Count;
        _storedStatus = stored.Contains("status") ? stored["status"].AsString : string.Empty;
        _storedNameIsCiphertext = contacts.Count > 0 && IsBase64(contacts[0].AsBsonDocument["name"].AsString);
    }

    [Fact] void should_apply_the_update_without_failing() => _error.ShouldBeNull();

    [Fact] void should_not_duplicate_the_child() => _storedContactCount.ShouldEqual(1);

    [Fact] void should_apply_the_unrelated_field_update() => _storedStatus.ShouldEqual("approved");

    [Fact] void should_keep_the_child_pii_as_ciphertext() => _storedNameIsCiphertext.ShouldBeTrue();

    AppendedEvent Event(EventSequenceNumber sequenceNumber) =>
        new(
            EventContext.From(EventStore, EventStoreNamespace, EventType.Unknown, EventSourceType.Default, Identifier, EventStreamType.All, EventStreamId.Default, sequenceNumber, CorrelationId.NotSet),
            new ExpandoObject());

    static ExpandoObject Contact() => Expando(("contactId", "contact-1"), ("name", PlaintextName), ("contactEmail", PlaintextEmail));
}
