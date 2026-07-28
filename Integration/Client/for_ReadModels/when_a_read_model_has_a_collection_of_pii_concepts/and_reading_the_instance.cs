// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_a_collection_of_pii_concepts.and_reading_the_instance.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_a_collection_of_pii_concepts;

/// <summary>
/// A <c>[PII]</c> concept keeps its classification when it is held in a collection. The schema for a collection
/// of concepts is built through a separate path from the scalar one, so without the element's compliance
/// metadata being carried onto the item schema the value would be persisted in the clear — the encrypted
/// scalar and the plaintext list element being the same concept type.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_reading_the_instance(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId PersonId { get; } = "pii-collection-person-1";
        public AliasesRecorded Event { get; private set; } = default!;
        public PersonAliases? Instance { get; private set; }
        public BsonDocument? StoredDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(AliasesRecorded)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(PersonAliases)];

        void Establish() => Event = new AliasesRecorded("Ada Lovelace", [new Alias("A. A. Lovelace"), new Alias("Ada Byron")]);

        async Task Because()
        {
            await EventStore.EventLog.Append(PersonId, Event);

            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            while (Instance?.Aliases is not { Count: 2 })
            {
                Instance = await EventStore.ReadModels.GetInstanceById<PersonAliases>(PersonId.Value);
                if (Instance?.Aliases is { Count: 2 }) break;
                await Task.Delay(200, cts.Token);
            }

            StoredDocument = await StoredReadModelDocument.Read(ChronicleFixture, "PersonAliases");
        }
    }

    [Fact] void should_release_every_alias_to_plaintext() =>
        Context.Instance!.Aliases.Select(_ => _.Value).Order().ShouldEqual(Context.Event.Aliases.Select(_ => _.Value).Order());

    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() =>
        (!Context.DocumentCanBeInspected || Context.StoredDocument is not null).ShouldBeTrue();

    [Fact] void should_store_every_alias_encrypted() =>
        StoredAliases?.Any(stored => Context.Event.Aliases.Any(alias => alias.Value == stored)).ShouldBeFalse();

    IEnumerable<string>? StoredAliases => Context.StoredDocument?["Aliases"].AsBsonArray.Select(_ => _.AsString);
}

[PII]
public record Alias(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator Alias(string value) => new(value);
}

[EventType]
public record AliasesRecorded(string Name, IReadOnlyList<Alias> Aliases);

[FromEvent<AliasesRecorded>]
public record PersonAliases(string Id, string Name, IReadOnlyList<Alias> Aliases);

#pragma warning restore SA1402
