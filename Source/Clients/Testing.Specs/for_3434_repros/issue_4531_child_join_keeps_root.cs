// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_4531;

/// <summary>
/// Child join (MembershipRosterProjection) arriving after two children exist: the root's other properties and the
/// sibling child must survive, as in persistent sinks.
/// </summary>
public class when_a_child_join_arrives_after_two_children_exist : Specification
{
    ReadModelScenario<MembershipRoster> _scenario;
    EventSourceId _groupId;
    MemberId _first;
    MemberId _second;
    MembershipRoster? _roster;

    void Establish()
    {
        _scenario = new ReadModelScenario<MembershipRoster>();
        _groupId = EventSourceId.New();
        _first = new MemberId(Guid.NewGuid());
        _second = new MemberId(Guid.NewGuid());
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_groupId).Events(new RosterOpened("Engineers"), new MemberEnrolled(_first), new MemberEnrolled(_second));
        await _scenario.Given.ForEventSource(new EventSourceId(_second.Value)).Events(new MemberProfileCreated("Grace"));
        _roster = _scenario.InstanceForEventSourceId(_groupId);
    }

    [Fact] void should_keep_the_group_name() => _roster!.GroupName.ShouldEqual("Engineers");
    [Fact] void should_keep_both_children() => _roster!.Members.Count().ShouldEqual(2);
    [Fact] void should_enrich_the_joined_child() => _roster!.Members.Single(_ => _.MemberId == _second).MemberName.ShouldEqual("Grace");
    [Fact] void should_have_one_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
