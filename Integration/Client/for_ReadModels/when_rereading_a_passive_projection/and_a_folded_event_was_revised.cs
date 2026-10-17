// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_rereading_a_passive_projection.and_a_folded_event_was_revised.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_rereading_a_passive_projection;

/// <summary>
/// Verifies that rereading a passive projection folds the revised stored content.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_a_folded_event_was_revised(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        EventSequenceNumber _nameSequenceNumber;

        public string MemberId { get; } = Guid.NewGuid().ToString();
        public RevisableMember? BeforeRevision { get; private set; }
        public RevisableMember? AfterRevision { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(RevisableMemberNamed)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(RevisableMember)];

        async Task Establish()
        {
            var named = await EventStore.EventLog.Append(MemberId, new RevisableMemberNamed("original name"));
            _nameSequenceNumber = named.SequenceNumber;
            BeforeRevision = await EventStore.ReadModels.GetInstanceById<RevisableMember>(MemberId);
        }

        async Task Because()
        {
            await this.ReviseEvent(_nameSequenceNumber, new RevisableMemberNamed("revised name"));
            AfterRevision = await EventStore.ReadModels.GetInstanceById<RevisableMember>(MemberId);
        }
    }

    [Fact] void should_have_folded_the_original_name_before_the_revision() => Context.BeforeRevision!.Name.ShouldEqual("original name");
    [Fact] void should_carry_the_revised_name() => Context.AfterRevision!.Name.ShouldEqual("revised name");
}

/// <summary>
/// A member's name was recorded.
/// </summary>
/// <param name="Name">The member's name.</param>
[EventType]
public record RevisableMemberNamed(string Name);

/// <summary>
/// A passive view of the member's name.
/// </summary>
/// <param name="Name">The member's name.</param>
[Passive]
[FromEvent<RevisableMemberNamed>]
public record RevisableMember(string Name);

#pragma warning restore SA1402
