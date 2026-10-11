// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_a_passive_projection_names_a_previous_generation : given.a_builder_with_generations
{
    ProjectionDefinition _definition;

    void Establish()
    {
        _builder.From<First>();
        _builder.Children(model => model.Items, children => children.From<First>());
        _builder.Nested(model => model.Nested, nested => nested.From<First>());
        _builder.Passive();
    }

    void Because() => _definition = _builder.BuildWithGenerationDelivery(EventGenerationDelivery.Pinned);

    [Fact] void should_keep_compatibility_delivery() => _definition.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_not_add_a_root_from_clause() => _definition.From.ShouldBeEmpty();
    [Fact] void should_not_add_a_child_from_clause() => _definition.Children.Values.Single().From.ShouldBeEmpty();
    [Fact] void should_not_add_a_nested_from_clause() => _definition.Nested.Values.Single().From.ShouldBeEmpty();
}
