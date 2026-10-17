// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_rereading_a_passive_projection.and_the_subject_was_erased.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_rereading_a_passive_projection;

/// <summary>
/// Verifies that rereading a passive projection no longer releases the erased subject's personal data.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_subject_was_erased(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public string MemberId { get; } = Guid.NewGuid().ToString();
        public ErasableMember? BeforeErasure { get; private set; }
        public ErasableMember? AfterErasure { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(ErasableMemberRegistered)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(ErasableMember)];

        async Task Establish()
        {
            await EventStore.EventLog.Append(MemberId, new ErasableMemberRegistered("Ada Lovelace"));
            BeforeErasure = await EventStore.ReadModels.GetInstanceById<ErasableMember>(MemberId);
        }

        async Task Because()
        {
            await EventStore.PII.DeleteEncryptionKeyFor(MemberId);
            AfterErasure = await EventStore.ReadModels.GetInstanceById<ErasableMember>(MemberId);
        }
    }

    [Fact] void should_release_the_full_name_before_the_erasure() => Context.BeforeErasure!.FullName.ShouldEqual("Ada Lovelace");
    [Fact] void should_blank_the_full_name_after_the_erasure() => Context.AfterErasure!.FullName.ShouldEqual(string.Empty);
}

/// <summary>
/// A member's personal name was registered.
/// </summary>
/// <param name="FullName">The member's personal name.</param>
[EventType]
public record ErasableMemberRegistered([property: PII] string FullName);

/// <summary>
/// A passive view of a member's personal name.
/// </summary>
/// <param name="FullName">The member's personal name.</param>
[Passive]
[FromEvent<ErasableMemberRegistered>]
public record ErasableMember([property: PII] string FullName);

#pragma warning restore SA1402
