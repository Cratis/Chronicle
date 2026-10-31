// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_pinned_child_and_nested_clauses_name_a_previous_generation : given.a_builder_with_generations
{
    ProjectionDefinition _definition;

    void Establish()
    {
        _builder.Children(model => model.Items, children => children
            .IdentifiedBy(model => model.Name)
            .From<First>()
            .Join<First>()
            .RemovedWith<First>()
            .RemovedWithJoin<First>());
        _builder.Nested(model => model.Nested, nested => nested.From<First>().ClearWith<First>());
    }

    void Because() => _definition = _builder.BuildWithGenerationDelivery(EventGenerationDelivery.Pinned);

    [Fact] void should_pin_the_child_from_generation() => _definition.Children.Values.Single().From.Keys.Single().Generation.ShouldEqual(1U);
    [Fact] void should_pin_the_child_join_generation() => _definition.Children.Values.Single().Join.Keys.Single().Generation.ShouldEqual(1U);
    [Fact] void should_pin_the_child_removal_generation() => _definition.Children.Values.Single().RemovedWith.Keys.Single().Generation.ShouldEqual(1U);
    [Fact] void should_pin_the_child_join_removal_generation() => _definition.Children.Values.Single().RemovedWithJoin.Keys.Single().Generation.ShouldEqual(1U);
    [Fact] void should_pin_the_nested_from_generation() => _definition.Nested.Values.Single().From.Keys.Single().Generation.ShouldEqual(1U);
    [Fact] void should_pin_the_nested_clear_generation() => _definition.Nested.Values.Single().RemovedWith.Keys.Single().Generation.ShouldEqual(1U);
}
