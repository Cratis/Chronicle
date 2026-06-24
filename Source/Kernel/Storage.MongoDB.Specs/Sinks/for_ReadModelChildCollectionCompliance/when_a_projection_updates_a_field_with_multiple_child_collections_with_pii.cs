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
/// Edge case for the projection collapse with more than one PII child collection. An unrelated top-level
/// update re-encrypts every PII member across both collections, yielding two independent whole-collection
/// replacements (<c>$set contacts</c> and <c>$set tags</c>). They target different collections so there is
/// no conflict; both must apply, leaving both collections intact and ciphertext at rest.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_projection_updates_a_field_with_multiple_child_collections_with_pii(MongoDBFixture fixture)
    : given.a_child_collection_compliance_scenario(fixture)
{
    Exception? _error;
    int _storedContactCount;
    int _storedTagCount;
    string _storedStatus = string.Empty;
    bool _contactIsCiphertext;
    bool _tagIsCiphertext;

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
                  "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] }
                }
              }
            },
            "tags": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "tagId": { "type": "string" },
                  "label": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] }
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

        var insert = new Changeset<AppendedEvent, ExpandoObject>(ObjectComparer, Event(EventSequenceNumber.First), new ExpandoObject());
        insert.AddChild("contacts", Contact());
        insert.AddChild("tags", Tag());
        await step.Perform(projection, new ProjectionEventContext(Key, insert.Incoming, insert, ProjectionOperationType.None, false));
        await Sink.ApplyChanges(Key, insert, EventSequenceNumber.First);

        var decryptedInitialState = Expando(
            ("id", Identifier),
            ("contacts", new object[] { Contact() }),
            ("tags", new object[] { Tag() }));
        var update = new Changeset<AppendedEvent, ExpandoObject>(ObjectComparer, Event(new EventSequenceNumber(1)), decryptedInitialState);
        update.Add(new PropertiesChanged<ExpandoObject>(null!, [new PropertyDifference("status", null, "approved")]));
        await step.Perform(projection, new ProjectionEventContext(Key, update.Incoming, update, ProjectionOperationType.None, false));

        _error = await Catch.Exception(() => Sink.ApplyChanges(Key, update, new EventSequenceNumber(1)));

        var stored = await StoredDocument();
        var contacts = stored["contacts"].AsBsonArray;
        var tags = stored["tags"].AsBsonArray;
        _storedContactCount = contacts.Count;
        _storedTagCount = tags.Count;
        _storedStatus = stored.Contains("status") ? stored["status"].AsString : string.Empty;
        _contactIsCiphertext = contacts.Count > 0 && IsBase64(contacts[0].AsBsonDocument["name"].AsString);
        _tagIsCiphertext = tags.Count > 0 && IsBase64(tags[0].AsBsonDocument["label"].AsString);
    }

    [Fact] void should_apply_the_update_without_failing() => _error.ShouldBeNull();

    [Fact] void should_keep_the_contact() => _storedContactCount.ShouldEqual(1);

    [Fact] void should_keep_the_tag() => _storedTagCount.ShouldEqual(1);

    [Fact] void should_apply_the_unrelated_field_update() => _storedStatus.ShouldEqual("approved");

    [Fact] void should_keep_the_contact_pii_ciphertext() => _contactIsCiphertext.ShouldBeTrue();

    [Fact] void should_keep_the_tag_pii_ciphertext() => _tagIsCiphertext.ShouldBeTrue();

    AppendedEvent Event(EventSequenceNumber sequenceNumber) =>
        new(
            EventContext.From(EventStore, EventStoreNamespace, EventType.Unknown, EventSourceType.Default, Identifier, EventStreamType.All, EventStreamId.Default, sequenceNumber, CorrelationId.NotSet),
            new ExpandoObject());

    static ExpandoObject Contact() => Expando(("contactId", "contact-1"), ("name", "Jane Doe"));

    static ExpandoObject Tag() => Expando(("tagId", "tag-1"), ("label", "Sensitive"));
}
