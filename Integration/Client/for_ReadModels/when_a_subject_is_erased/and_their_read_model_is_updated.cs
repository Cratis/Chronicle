// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_subject_is_erased.and_their_read_model_is_updated.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_subject_is_erased;

/// <summary>
/// A read model holding a person's personal data keeps updating after the person has been erased (#4453). The next
/// event after the erasure carries no personal data, but the projection re-protects the whole document before it is
/// stored, and that used to ask for a fresh key the erasure fence refuses - freezing the partition for good.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_their_read_model_is_updated(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId MemberId { get; } = "erased-member-update-1";
        public ErasedMemberRegistered Registered { get; } = new("Ada Lovelace", new ErasedMemberAddress("St James's Square", 1815, true), "registered");
        public ErasedMemberStatusChanged StatusChanged { get; } = new("moved");

        public ErasedMember? BeforeErasure { get; private set; }
        public ErasedMember? AfterUpdate { get; private set; }
        public IEnumerable<FailedPartition> FailedPartitions { get; private set; } = [];
        public BsonDocument? StoredDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(ErasedMemberRegistered), typeof(ErasedMemberStatusChanged)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(ErasedMember)];

        async Task Because()
        {
            var projectionId = EventStore.Projections.GetProjectionIdForModel<ErasedMember>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            var registered = await EventStore.EventLog.Append(MemberId, Registered);
            await handler.WaitTillReachesEventSequenceNumber(registered.SequenceNumber);
            BeforeErasure = await EventStore.ReadModels.GetInstanceById<ErasedMember>(MemberId.Value);

            await EventStore.PII.DeleteEncryptionKeyFor(MemberId.Value);

            var statusChanged = await EventStore.EventLog.Append(MemberId, StatusChanged);
            await handler.WaitTillReachesEventSequenceNumber(statusChanged.SequenceNumber);

            FailedPartitions = await handler.GetFailedPartitions();
            AfterUpdate = await EventStore.ReadModels.GetInstanceById<ErasedMember>(MemberId.Value);
            StoredDocument = await StoredReadModelDocument.Read(ChronicleFixture, "ErasedMembers");
        }
    }

    [Fact] void should_release_the_name_before_the_erasure() => Context.BeforeErasure!.Name.ShouldEqual(Context.Registered.Name);
    [Fact] void should_not_fail_the_partition() => Context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_store_the_update() => Context.AfterUpdate!.Status.ShouldEqual(Context.StatusChanged.Status);
    [Fact] void should_keep_the_name_erased() => Context.AfterUpdate!.Name.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_street_erased() => Context.AfterUpdate!.Address.Street.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_postal_code_erased() => Context.AfterUpdate!.Address.PostalCode.ShouldEqual(0);
    [Fact] void should_keep_the_verification_flag_erased() => Context.AfterUpdate!.Address.Verified.ShouldBeFalse();
    [Fact] void should_store_no_personal_data_when_the_backend_allows_it() => (!Context.DocumentCanBeInspected || Context.StoredDocument?.ToJson().Contains(Context.Registered.Name) == false).ShouldBeTrue();
}

/// <summary>
/// Where a person lives. Every member is personal, so the marker lands on each of them.
/// </summary>
/// <param name="Street">The street.</param>
/// <param name="PostalCode">The postal code.</param>
/// <param name="Verified">Whether the address was verified.</param>
[PII]
public record ErasedMemberAddress(string Street, int PostalCode, bool Verified);

/// <summary>
/// A person was registered.
/// </summary>
/// <param name="Name">The person's name.</param>
/// <param name="Address">The person's address.</param>
/// <param name="Status">The person's status, which is not personal.</param>
[EventType]
public record ErasedMemberRegistered([property: PII] string Name, ErasedMemberAddress Address, string Status);

/// <summary>
/// A person's status changed. It carries nothing personal.
/// </summary>
/// <param name="Status">The new status.</param>
[EventType]
public record ErasedMemberStatusChanged(string Status);

/// <summary>
/// A person, holding their personal data beside a status that is not personal.
/// </summary>
/// <param name="Id">The person's identifier.</param>
/// <param name="Name">The person's name.</param>
/// <param name="Address">The person's address.</param>
/// <param name="Status">The person's status.</param>
[FromEvent<ErasedMemberRegistered>]
[FromEvent<ErasedMemberStatusChanged>]
public record ErasedMember(string Id, [property: PII] string Name, ErasedMemberAddress Address, string Status);

#pragma warning restore SA1402
