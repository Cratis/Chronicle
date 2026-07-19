// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ConstraintsStorage;

public class when_saving_a_definition_with_an_existing_name : given.a_constraints_storage
{
    static readonly ConstraintName _name = "some-constraint";
    IConstraintDefinition _first;
    IConstraintDefinition _second;
    IEnumerable<IConstraintDefinition> _definitions;

    void Establish()
    {
        _first = Substitute.For<IConstraintDefinition>();
        _first.Name.Returns(_name);
        _second = Substitute.For<IConstraintDefinition>();
        _second.Name.Returns(_name);
    }

    async Task Because()
    {
        await _storage.SaveDefinition(_first);
        await _storage.SaveDefinition(_second);
        _definitions = await _storage.GetDefinitions();
    }

    [Fact] void should_have_only_one_definition() => _definitions.Count().ShouldEqual(1);
    [Fact] void should_have_replaced_with_the_latest_definition() => _definitions.Single().ShouldEqual(_second);
}
