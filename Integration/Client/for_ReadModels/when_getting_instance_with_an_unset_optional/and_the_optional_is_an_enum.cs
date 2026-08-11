// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_with_an_unset_optional.and_the_optional_is_an_enum.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_with_an_unset_optional;

/// <summary>
/// The enum case of the unset optional, driven through the compliance write path and asserted on the document at
/// rest. A read model carrying a <c>[PII]</c> value has its whole state converted through its registered schema
/// on the way into the sink, and every schema property with no value is offered a type default there. An optional
/// one must be offered none, so its field is simply never written - and after a projection replay, never stamped
/// onto every stored record.
/// </summary>
/// <remarks>
/// The enum declares a zero member, and that is the whole reason this spec can fail. The obvious subject - a
/// 1-based enum, the one the defect was reported against - cannot be observed here at all: the converter encodes
/// an enum by member name, so a value outside the declared list has no name to write and the field stays absent
/// whether the default was withheld deliberately or refused incidentally. Both answers look identical at rest, so
/// a spec built on that subject would be green against a kernel with the fix removed. An enum whose zero <em>is</em>
/// declared has a name for it, and the difference between withheld and written becomes a difference in the stored
/// document.
/// <para>
/// The read-back assertion is deliberately paired with the at-rest one rather than trusted alone - a round trip
/// resolves through the same member list and cannot, on its own, separate an absent field from a stored value the
/// schema forbids.
/// </para>
/// </remarks>
/// <param name="context">The scenario context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_optional_is_an_enum(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        const string CollectionName = "SignedContracts";

        public EventSourceId ContractId { get; } = "unset-optional-enum-contract-1";
        public SignedContract? Instance { get; private set; }
        public BsonDocument? StoredDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(ContractSigned), typeof(ContractRejected)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(SignedContract)];

        async Task Because()
        {
            // Only the creating event fires. ContractRejected - the source of the optional Outcome - never does.
            await EventStore.EventLog.Append(ContractId, new ContractSigned("Ada Lovelace"));

            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            while (Instance?.Signer is null)
            {
                Instance = await EventStore.ReadModels.GetInstanceById<SignedContract>(ContractId.Value);
                if (Instance?.Signer is not null) break;
                await Task.Delay(200, cts.Token);
            }

            StoredDocument = await StoredReadModelDocument.Read(ChronicleFixture, CollectionName);
        }
    }

    [Fact] void should_return_the_instance() => Context.Instance.ShouldNotBeNull();
    [Fact] void should_release_the_pii_property() => Context.Instance!.Signer.Value.ShouldEqual("Ada Lovelace");
    [Fact] void should_read_back_the_unset_optional_as_null() => Context.Instance!.Outcome.ShouldBeNull();
    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() => (!Context.DocumentCanBeInspected || Context.StoredDocument is not null).ShouldBeTrue();
    [Fact] void should_not_store_an_answer_the_read_model_never_gave() => (!Context.DocumentCanBeInspected || !Context.StoredDocument!.Contains(nameof(SignedContract.Outcome))).ShouldBeTrue();
    [Fact] void should_store_the_property_that_was_set() => (!Context.DocumentCanBeInspected || Context.StoredDocument!.Contains(nameof(SignedContract.Signer))).ShouldBeTrue();
}

[PII]
public record SignerName(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator SignerName(string value) => new(value);
}

public enum SigningOutcome
{
    NotSet = 0,
    RejectedBySigner = 1,
    Expired = 2,
    Withdrawn = 3
}

[EventType]
public record ContractSigned(SignerName Signer);

[EventType]
public record ContractRejected(SigningOutcome Outcome);

[FromEvent<ContractSigned>]
public record SignedContract(
    string Id,
    SignerName Signer,

    [property: SetFrom<ContractRejected>(nameof(ContractRejected.Outcome))]
    SigningOutcome? Outcome);

#pragma warning restore SA1402
