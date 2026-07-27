// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_a_collection_of_pii_concepts.and_the_elements_are_objects.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_a_collection_of_pii_concepts;

/// <summary>
/// The elements of the collection are objects rather than scalars, and each carries a <c>[PII]</c> member
/// beside a plain one. The walk has to reach into every element and encrypt only the personal member.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_elements_are_objects(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId CaseId { get; } = "pii-element-objects-case-1";
        public ContactsRecorded Event { get; private set; } = default!;
        public CaseContacts? Instance { get; private set; }
        public BsonDocument? StoredDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(ContactsRecorded)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(CaseContacts)];

        void Establish() => Event = new ContactsRecorded(
            "Case 42",
            [new Contact("primary", "ada@example.com"), new Contact("secondary", "grace@example.com")]);

        async Task Because()
        {
            await EventStore.EventLog.Append(CaseId, Event);

            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            while (Instance?.Contacts is not { Count: 2 })
            {
                Instance = await EventStore.ReadModels.GetInstanceById<CaseContacts>(CaseId.Value);
                if (Instance?.Contacts is { Count: 2 }) break;
                await Task.Delay(200, cts.Token);
            }

            StoredDocument = await StoredReadModelDocument.Read(ChronicleFixture, "CaseContacts");
        }
    }

    [Fact] void should_release_every_email_to_plaintext() =>
        Context.Instance!.Contacts.Select(_ => _.Email).Order().ShouldEqual(Context.Event.Contacts.Select(_ => _.Email).Order());

    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() =>
        (!Context.DocumentCanBeInspected || Context.StoredDocument is not null).ShouldBeTrue();

    [Fact] void should_store_every_email_encrypted() =>
        StoredContacts?.Select(_ => _["Email"].AsString)
            .Any(stored => Context.Event.Contacts.Any(contact => contact.Email == stored)).ShouldBeFalse();

    [Fact] void should_store_the_non_pii_member_in_the_clear() =>
        StoredContacts?.Select(_ => _["Kind"].AsString).Order().ShouldEqual(Context.Event.Contacts.Select(_ => _.Kind).Order());

    IEnumerable<BsonDocument>? StoredContacts => Context.StoredDocument?["Contacts"].AsBsonArray.Select(_ => _.AsBsonDocument);
}

public record Contact(string Kind, [property: PII] string Email);

[EventType]
public record ContactsRecorded(string Title, IReadOnlyList<Contact> Contacts);

[FromEvent<ContactsRecorded>]
public record CaseContacts(string Id, string Title, IReadOnlyList<Contact> Contacts);

#pragma warning restore SA1402
