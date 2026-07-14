// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_a_child_source;

/// <summary>
/// A child-level <c>[Join]</c> enriches the child row when the join-source event is seeded BEFORE the child row
/// is created (join-source-first order). This is the ordering the engine's row-creation-time backfill already
/// handles; asserting it here proves the child-first fix does not regress it — both orders produce IDENTICAL
/// results with exactly one child.
/// </summary>
public class and_join_source_is_seeded_first : Specification
{
    ReadModelScenario<MembershipRoster> _scenario;
    EventSourceId _groupId;
    MemberId _memberId;
    MembershipRoster? _roster;

    void Establish()
    {
        _scenario = new ReadModelScenario<MembershipRoster>();
        _groupId = new EventSourceId(Guid.NewGuid());
        _memberId = new MemberId(Guid.NewGuid());
    }

    async Task Because()
    {
        // The join source (member profile) exists before the member is enrolled into the group.
        await _scenario.Given
            .ForEventSource(new EventSourceId(_memberId.Value))
            .Events(new MemberProfileCreated("Ada"));

        await _scenario.Given
            .ForEventSource(_groupId)
            .Events(new RosterOpened("Engineers"), new MemberEnrolled(_memberId));

        _roster = _scenario.InstanceForEventSourceId(_groupId);
    }

    [Fact] void should_materialize_the_roster() => _roster.ShouldNotBeNull();
    [Fact] void should_keep_the_group_name() => _roster!.GroupName.ShouldEqual("Engineers");
    [Fact] void should_have_exactly_one_member() => _roster!.Members.Count().ShouldEqual(1);
    [Fact] void should_key_the_member_by_its_id() => _roster!.Members.Single().MemberId.ShouldEqual(_memberId);
    [Fact] void should_backfill_the_member_name() => _roster!.Members.Single().MemberName.ShouldEqual("Ada");
    [Fact] void should_materialize_only_the_roster_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
