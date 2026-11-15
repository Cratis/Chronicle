// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario.when_appending_events;

public class and_a_person_is_removed : Specification
{
    static readonly Guid _personId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    static readonly Guid _groupId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    GraphScenario _scenario;
    ScenarioPerson? _person;
    IReadOnlyList<ScenarioMemberOf> _edges;
    ScenarioGroup? _group;

    async Task Because()
    {
        _scenario = new GraphScenario();
        await _scenario.Append(_personId, new ScenarioPersonRegistered(_personId, "Ada"), new ScenarioMemberAdded(_personId, _groupId));
        await _scenario.Append(_personId, new ScenarioPersonRemoved(_personId));
        _person = await _scenario.Node<ScenarioPerson>(_personId);
        _edges = await _scenario.Edges<ScenarioMemberOf>();
        _group = await _scenario.Node<ScenarioGroup>(_groupId);
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_delete_the_node() => _person.ShouldBeNull();
    [Fact] void should_detach_its_edges() => _edges.ShouldBeEmpty();
    [Fact] void should_leave_the_other_end_node() => _group.ShouldNotBeNull();
}
