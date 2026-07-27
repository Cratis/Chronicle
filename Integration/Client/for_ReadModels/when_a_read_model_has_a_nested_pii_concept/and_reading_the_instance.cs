// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_a_nested_pii_concept.and_reading_the_instance.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_a_nested_pii_concept;

/// <summary>
/// A read model whose property is a value object holding a <c>[PII]</c> concept. The compliance walk has to
/// reach the value inside the value object on both sides: encrypt it on the way into the sink, and release it
/// on the way back out. An asymmetry here does not surface as a missing value — it stores plaintext and then
/// throws on release — so the document at rest is asserted directly rather than only the round trip.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_reading_the_instance(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId PersonId { get; } = "nested-pii-person-1";
        public IdentityVerified Event { get; private set; } = default!;
        public ExpressVerification? Instance { get; private set; }
        public BsonDocument? StoredDocument { get; private set; }
        public ExpressVerification? ReleasedFromCollection { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(IdentityVerified)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(ExpressVerification)];

        void Establish() => Event = new IdentityVerified("Ada Lovelace", new VerifiedDateOfBirth("1815-12-10", "bankid"));

        async Task Because()
        {
            await EventStore.EventLog.Append(PersonId, Event);

            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            while (Instance?.DateOfBirth is null)
            {
                Instance = await EventStore.ReadModels.GetInstanceById<ExpressVerification>(PersonId.Value);
                if (Instance?.DateOfBirth is not null) break;
                await Task.Delay(200, cts.Token);
            }

            StoredDocument = await ChronicleFixture.ReadModels.Database
                .GetCollection<BsonDocument>(CollectionName)
                .Find(Builders<BsonDocument>.Filter.Empty)
                .FirstOrDefaultAsync();

            // The document read straight out of the sink still holds ciphertext; releasing it is the client-side
            // path an application takes when it queries the collection itself rather than going through the kernel.
            var fromCollection = await ChronicleFixture.ReadModels.Database
                .GetCollection<ExpressVerification>(CollectionName)
                .Find(Builders<ExpressVerification>.Filter.Empty)
                .FirstOrDefaultAsync();

            ReleasedFromCollection = await EventStore.ReadModels.Release(fromCollection);
        }

        const string CollectionName = "ExpressVerifications";
    }

    [Fact] void should_return_the_instance() => Context.Instance.ShouldNotBeNull();
    [Fact] void should_release_the_nested_pii_to_plaintext() => Context.Instance!.DateOfBirth.DateOfBirth.ShouldEqual(Context.Event.DateOfBirth.DateOfBirth);
    [Fact] void should_keep_the_non_pii_sibling() => Context.Instance!.DateOfBirth.VerifiedBy.ShouldEqual(Context.Event.DateOfBirth.VerifiedBy);
    [Fact] void should_store_the_nested_pii_encrypted() => Context.StoredDocument!["DateOfBirth"]["DateOfBirth"].AsString.ShouldNotEqual(Context.Event.DateOfBirth.DateOfBirth.Value);
    [Fact] void should_store_the_non_pii_sibling_in_the_clear() => Context.StoredDocument!["DateOfBirth"]["VerifiedBy"].AsString.ShouldEqual(Context.Event.DateOfBirth.VerifiedBy.Value);
    [Fact] void should_release_nested_pii_read_through_the_collection() => Context.ReleasedFromCollection!.DateOfBirth.DateOfBirth.ShouldEqual(Context.Event.DateOfBirth.DateOfBirth);
}

[PII]
public record DateOfBirth(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator DateOfBirth(string value) => new(value);
}

public record VerifiedBy(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator VerifiedBy(string value) => new(value);
}

public record VerifiedDateOfBirth(DateOfBirth DateOfBirth, VerifiedBy VerifiedBy);

[EventType]
public record IdentityVerified(string Name, VerifiedDateOfBirth DateOfBirth);

[FromEvent<IdentityVerified>]
public record ExpressVerification(string Id, string Name, VerifiedDateOfBirth DateOfBirth);

#pragma warning restore SA1402
