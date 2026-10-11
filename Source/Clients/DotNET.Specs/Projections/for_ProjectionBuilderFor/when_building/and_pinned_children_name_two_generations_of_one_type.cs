// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_pinned_children_name_two_generations_of_one_type : given.a_builder_with_generations
{
    Exception _error;

    void Establish()
    {
        _builder.Children(model => model.Items, child => child.From<First>());
        _builder.Children(model => model.OtherItems, child => child.From<Second>());
    }

    void Because() => _error = Catch.Exception(() => _builder.BuildWithGenerationDelivery(EventGenerationDelivery.Pinned));

    [Fact] void should_reject_ambiguous_pins_across_children() => _error.ShouldBeOfExactType<MultipleGenerationsOfEventTypeInObserver>();
}
