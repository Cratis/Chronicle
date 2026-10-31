// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_compatibility_child_and_nested_from_name_a_previous_generation : given.a_builder_with_generations
{
    ProjectionDefinition _definition;

    void Establish()
    {
        _builder.Children(model => model.Items, children => children.From<First>());
        _builder.Nested(model => model.Nested, nested => nested.From<First>());
    }

    void Because() => _definition = _builder.Build();

    [Fact] void should_not_add_a_child_from_clause() => _definition.Children.Values.Single().From.ShouldBeEmpty();
    [Fact] void should_not_add_a_nested_from_clause() => _definition.Nested.Values.Single().From.ShouldBeEmpty();
}
