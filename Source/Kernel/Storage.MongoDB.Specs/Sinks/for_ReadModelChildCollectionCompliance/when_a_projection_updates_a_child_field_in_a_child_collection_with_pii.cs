// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChildCollectionCompliance;

/// <summary>
/// Robustness adjacency for the projection collapse: positionally updating a non-PII field on an existing
/// child while the same child collection carries scalar <c>[PII]</c>. The projection emits a positional
/// <c>$set contacts.$[elem].label</c> while <see cref="EncryptChangeset"/> re-encrypts and collapses the
/// PII members into a whole-collection <c>$set contacts</c>. Both target the same collection in one update;
/// the sink must apply them without a Mongo path conflict, with the field updated and the PII ciphertext.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_projection_updates_a_child_field_in_a_child_collection_with_pii(MongoDBFixture fixture)
    : given.a_child_collection_compliance_scenario(fixture)
{
    Exception? _error;
    string _storedLabel = string.Empty;
    bool _storedNameIsCiphertext;

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
                  "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] },
                  "label": { "type": "string" }
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

        // Insert leg — add the child carrying scalar [PII] with an initial non-PII label.
        var insert = new Changeset<AppendedEvent, ExpandoObject>(ObjectComparer, Event(EventSequenceNumber.First), new ExpandoObject());
        insert.AddChild("contacts", Contact("old"));
        await step.Perform(projection, new ProjectionEventContext(Key, insert.Incoming, insert, ProjectionOperationType.None, false));
        await Sink.ApplyChanges(Key, insert, EventSequenceNumber.First);

        // Update leg — a positional child-field update on the existing child (as a projection produces),
        // alongside the re-encryption of the child's PII over the whole snapshot.
        var decryptedInitialState = Expando(("id", Identifier), ("contacts", new object[] { Contact("new") }));
        var update = new Changeset<AppendedEvent, ExpandoObject>(ObjectComparer, Event(new EventSequenceNumber(1)), decryptedInitialState);
        var indexers = new ArrayIndexers([new ArrayIndexer(new PropertyPath("[contacts]"), new PropertyPath("contactId"), "contact-1")]);
        update.Add(new PropertiesChanged<ExpandoObject>(null!, [new PropertyDifference(new PropertyPath("[contacts].label"), "old", "new", indexers)]));
        await step.Perform(projection, new ProjectionEventContext(Key, update.Incoming, update, ProjectionOperationType.None, false));

        _error = await Catch.Exception(() => Sink.ApplyChanges(Key, update, new EventSequenceNumber(1)));

        var contact = (await StoredDocument())["contacts"].AsBsonArray[0].AsBsonDocument;
        _storedLabel = contact.Contains("label") ? contact["label"].AsString : string.Empty;
        _storedNameIsCiphertext = IsBase64(contact["name"].AsString);
    }

    [Fact] void should_apply_the_update_without_failing() => _error.ShouldBeNull();

    [Fact] void should_apply_the_child_field_update() => _storedLabel.ShouldEqual("new");

    [Fact] void should_keep_the_child_pii_as_ciphertext() => _storedNameIsCiphertext.ShouldBeTrue();

    AppendedEvent Event(EventSequenceNumber sequenceNumber) =>
        new(
            EventContext.From(EventStore, EventStoreNamespace, EventType.Unknown, EventSourceType.Default, Identifier, EventStreamType.All, EventStreamId.Default, sequenceNumber, CorrelationId.NotSet),
            new ExpandoObject());

    static ExpandoObject Contact(string label) => Expando(("contactId", "contact-1"), ("name", "Jane Doe"), ("label", label));
}
