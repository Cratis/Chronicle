// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario.when_appending_events;

public class and_a_member_is_added : Specification
{
    static readonly Guid _personId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    static readonly Guid _groupId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    GraphScenario _scenario;
    ScenarioMemberOf? _edge;
    ScenarioGroup? _group;
    ScenarioPerson? _person;

    async Task Because()
    {
        _scenario = new GraphScenario();
        await _scenario.Append(_groupId, new ScenarioGroupCreated(_groupId, "Engineering"));
        await _scenario.Append(_personId, new ScenarioMemberAdded(_personId, _groupId));
        _edge = await _scenario.Edge<ScenarioMemberOf>(_personId, _groupId);
        _group = await _scenario.Node<ScenarioGroup>(_groupId);
        _person = await _scenario.Node<ScenarioPerson>(_personId);
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_have_the_edge() => _edge.ShouldNotBeNull();
    [Fact] void should_have_the_keys_of_the_end_nodes_on_the_edge() => (_edge!.PersonId, _edge.GroupId).ShouldEqual((_personId, _groupId));
    [Fact] void should_have_set_the_edge_property_from_the_event_context() => _edge!.Since.ShouldNotEqual(default);
    [Fact] void should_count_the_member_on_the_group() => _group!.MemberCount.ShouldEqual(1);
    [Fact] void should_keep_the_group_properties() => _group!.Name.ShouldEqual("Engineering");
    [Fact] void should_have_ensured_the_source_node() => _person.ShouldNotBeNull();
}
