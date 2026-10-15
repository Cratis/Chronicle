// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_removing.child_removed_with_join_updates_every_mappings_of_its_parents.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_removing;

[Collection(ChronicleCollection.Name)]
public class child_removed_with_join_updates_every_mappings_of_its_parents(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public static readonly Guid FirstMemberId = Guid.Parse("4f1d3c1e-6d38-4f43-9a54-0bb4f4a0d2a1");
        public static readonly Guid SecondMemberId = Guid.Parse("a4e5c1f2-7b0c-4d0f-8c3e-1f5c2b9d7e63");
        public static readonly Guid UnrelatedMemberId = Guid.Parse("c3b2a190-8f7e-4d6c-b5a4-9382716f5e4d");
        public static readonly Guid DisbandedTeamId = Guid.Parse("0d9c8b7a-6f5e-4d3c-2b1a-0f9e8d7c6b5a");
        public static readonly Guid KeptTeamId = Guid.Parse("1e2d3c4b-5a69-4788-9a0b-1c2d3e4f5a6b");
        public static readonly DateTimeOffset JoinedAt = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        public static readonly DateTimeOffset DisbandedAt = new(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);

        public AuditedMember FirstMember;
        public AuditedMember SecondMember;
        public AuditedMember UnrelatedMember;
        public IEnumerable<AuditedMember> Instances;

        public override IEnumerable<Type> EventTypes => [typeof(AuditedMemberCreated), typeof(AuditedMemberJoinedTeam), typeof(AuditedTeamDisbanded)];
        public override IEnumerable<Type> Projections => [typeof(AuditedMemberProjection)];

        async Task Because()
        {
            var handler = EventStore.Projections.GetHandlerFor<AuditedMemberProjection>();
            await handler.WaitTillSubscribed();

            // Members and teams are separate partitions, which the observer may process in any order. Let every
            // event land before the next so the read models follow the event order rather than a partition race.
            async Task AppendAndWait(Guid eventSourceId, object @event, DateTimeOffset occurred)
            {
                var result = await EventStore.EventLog.Append(eventSourceId, @event, occurred: occurred);
                await result.WaitForCompletion();
            }

            foreach (var memberId in (Guid[])[FirstMemberId, SecondMemberId, UnrelatedMemberId])
            {
                await AppendAndWait(memberId, new AuditedMemberCreated(memberId.ToString()), JoinedAt.AddDays(-1));
            }

            await AppendAndWait(DisbandedTeamId, new AuditedMemberJoinedTeam(FirstMemberId), JoinedAt);
            await AppendAndWait(DisbandedTeamId, new AuditedMemberJoinedTeam(SecondMemberId), JoinedAt);
            await AppendAndWait(KeptTeamId, new AuditedMemberJoinedTeam(UnrelatedMemberId), JoinedAt);
            await AppendAndWait(DisbandedTeamId, new AuditedTeamDisbanded(), DisbandedAt);

            FirstMember = await EventStore.ReadModels.GetInstanceById<AuditedMember>(FirstMemberId.ToString());
            SecondMember = await EventStore.ReadModels.GetInstanceById<AuditedMember>(SecondMemberId.ToString());
            UnrelatedMember = await EventStore.ReadModels.GetInstanceById<AuditedMember>(UnrelatedMemberId.ToString());
            Instances = await EventStore.ReadModels.GetInstances<AuditedMember>();
        }
    }

    [Fact] void should_remove_the_team_from_the_first_member() => Context.FirstMember.Teams.ShouldBeEmpty();
    [Fact] void should_remove_the_team_from_the_second_member() => Context.SecondMember.Teams.ShouldBeEmpty();
    [Fact] void should_update_the_first_member_from_the_removal() => Context.FirstMember.LastUpdated.ShouldEqual(context.DisbandedAt);
    [Fact] void should_update_the_second_member_from_the_removal() => Context.SecondMember.LastUpdated.ShouldEqual(context.DisbandedAt);
    [Fact] void should_keep_the_team_of_the_unrelated_member() => Context.UnrelatedMember.Teams.Select(_ => _.TeamId).ShouldContainOnly(context.KeptTeamId);
    [Fact] void should_not_update_the_unrelated_member() => Context.UnrelatedMember.LastUpdated.ShouldEqual(context.JoinedAt);
    [Fact] void should_not_create_a_member_for_the_team() => Context.Instances.Select(_ => _.Id).ShouldContainOnly(context.FirstMemberId, context.SecondMemberId, context.UnrelatedMemberId);
}

[EventType]
public record AuditedMemberCreated(string Name);

[EventType]
public record AuditedMemberJoinedTeam(Guid MemberId);

[EventType]
public record AuditedTeamDisbanded();

public record AuditedMember(Guid Id, string Name, DateTimeOffset LastUpdated, IEnumerable<AuditedMembership> Teams);

public record AuditedMembership(Guid TeamId);

public class AuditedMemberProjection : IProjectionFor<AuditedMember>
{
    public void Define(IProjectionBuilderFor<AuditedMember> builder) => builder
        .FromEvery(_ => _.Set(m => m.LastUpdated).ToEventContextProperty(e => e.Occurred))
        .From<AuditedMemberCreated>()
        .Children(_ => _.Teams, _ => _
            .IdentifiedBy(e => e.TeamId)
            .From<AuditedMemberJoinedTeam>(b => b
                .UsingParentKey(e => e.MemberId))
            .RemovedWithJoin<AuditedTeamDisbanded>());
}

#pragma warning restore SA1402 // File may only contain a single type
