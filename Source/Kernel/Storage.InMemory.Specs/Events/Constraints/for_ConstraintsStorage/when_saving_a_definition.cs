// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ConstraintsStorage;

public class when_saving_a_definition : given.a_constraints_storage
{
    IConstraintDefinition _definition;
    IEnumerable<IConstraintDefinition> _definitions;

    void Establish()
    {
        _definition = Substitute.For<IConstraintDefinition>();
        _definition.Name.Returns(new ConstraintName("some-constraint"));
    }

    async Task Because()
    {
        await _storage.SaveDefinition(_definition);
        _definitions = await _storage.GetDefinitions();
    }

    [Fact] void should_return_the_saved_definition() => _definitions.ShouldContain(_definition);
}
