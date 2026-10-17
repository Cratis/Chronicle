// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_rereading_a_passive_projection.and_a_folded_event_was_redacted.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_rereading_a_passive_projection;

/// <summary>
/// Verifies that rereading a passive projection reflects an in-place redaction.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_a_folded_event_was_redacted(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        EventSequenceNumber _nicknameSequenceNumber;

        public string MemberId { get; } = Guid.NewGuid().ToString();
        public RedactableMember? BeforeRedaction { get; private set; }
        public RedactableMember? AfterRedaction { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(RedactableMemberRegistered), typeof(RedactableMemberNicknamed)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(RedactableMember)];

        async Task Establish()
        {
            await EventStore.EventLog.Append(MemberId, new RedactableMemberRegistered("Ada Lovelace"));
            var nicknamed = await EventStore.EventLog.Append(MemberId, new RedactableMemberNicknamed("Countess"));
            _nicknameSequenceNumber = nicknamed.SequenceNumber;
            BeforeRedaction = await EventStore.ReadModels.GetInstanceById<RedactableMember>(MemberId);
        }

        async Task Because()
        {
            await this.RedactEvent(_nicknameSequenceNumber, "redacted by specification");
            AfterRedaction = await EventStore.ReadModels.GetInstanceById<RedactableMember>(MemberId);
        }
    }

    [Fact] void should_have_folded_the_nickname_before_the_redaction() => Context.BeforeRedaction!.Nickname.ShouldEqual("Countess");
    [Fact] void should_keep_the_unredacted_name() => Context.AfterRedaction!.Name.ShouldEqual("Ada Lovelace");
    [Fact] void should_no_longer_carry_the_redacted_nickname() => Context.AfterRedaction!.Nickname.ShouldBeNull();
}

/// <summary>
/// A member's name was registered.
/// </summary>
/// <param name="Name">The member's name.</param>
[EventType]
public record RedactableMemberRegistered(string Name);

/// <summary>
/// A member acquired a nickname.
/// </summary>
/// <param name="Nickname">The member's nickname.</param>
[EventType]
public record RedactableMemberNicknamed(string Nickname);

/// <summary>
/// A passive view of a member's name and nickname.
/// </summary>
/// <param name="Name">The member's name.</param>
/// <param name="Nickname">The optional nickname.</param>
[Passive]
[FromEvent<RedactableMemberRegistered>]
[FromEvent<RedactableMemberNicknamed>]
public record RedactableMember(string Name, string? Nickname);

#pragma warning restore SA1402
