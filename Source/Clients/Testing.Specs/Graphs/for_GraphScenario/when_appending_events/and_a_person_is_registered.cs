// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario.when_appending_events;

public class and_a_person_is_registered : Specification
{
    static readonly Guid _personId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    GraphScenario _scenario;
    ScenarioPerson? _person;
    IReadOnlyList<ScenarioPerson> _people;

    async Task Because()
    {
        _scenario = new GraphScenario();
        await _scenario.Append(_personId, new ScenarioPersonRegistered(_personId, "Ada"));
        _person = await _scenario.Node<ScenarioPerson>(_personId);
        _people = await _scenario.Nodes<ScenarioPerson>();
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_have_the_node() => _person.ShouldNotBeNull();
    [Fact] void should_have_mapped_the_properties() => _person!.Name.ShouldEqual("Ada");
    [Fact] void should_have_the_key() => _person!.PersonId.ShouldEqual(_personId);
    [Fact] void should_list_the_node() => _people.Count.ShouldEqual(1);
}
