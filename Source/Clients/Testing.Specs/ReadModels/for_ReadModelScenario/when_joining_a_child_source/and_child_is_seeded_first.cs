// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_a_child_source;

/// <summary>
/// A child-level <c>[Join]</c> enriches the EXISTING child row in place when the join-source event is seeded
/// AFTER the child row was created (child-first order). The join source arriving late must update the matching
/// child — matched by its identifier / join key — rather than appending a DUPLICATE child, mirroring the real
/// MongoDB sink.
/// </summary>
public class and_child_is_seeded_first : Specification
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
        await _scenario.Given
            .ForEventSource(_groupId)
            .Events(new RosterOpened("Engineers"), new MemberEnrolled(_memberId));

        // The join source (member profile) arrives AFTER the child row already exists — it must UPDATE the
        // existing child, never append a duplicate.
        await _scenario.Given
            .ForEventSource(new EventSourceId(_memberId.Value))
            .Events(new MemberProfileCreated("Ada"));

        _roster = _scenario.InstanceForEventSourceId(_groupId);
    }

    [Fact] void should_materialize_the_roster() => _roster.ShouldNotBeNull();
    [Fact] void should_keep_the_group_name() => _roster!.GroupName.ShouldEqual("Engineers");
    [Fact] void should_have_exactly_one_member() => _roster!.Members.Count().ShouldEqual(1);
    [Fact] void should_key_the_member_by_its_id() => _roster!.Members.Single().MemberId.ShouldEqual(_memberId);
    [Fact] void should_backfill_the_member_name() => _roster!.Members.Single().MemberName.ShouldEqual("Ada");
    [Fact] void should_materialize_only_the_roster_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
