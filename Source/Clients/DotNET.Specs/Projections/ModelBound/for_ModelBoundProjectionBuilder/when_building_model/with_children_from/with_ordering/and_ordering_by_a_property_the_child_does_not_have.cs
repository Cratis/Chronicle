// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from.with_ordering;

public class and_ordering_by_a_property_the_child_does_not_have : given.a_model_bound_projection_builder_for_ordered_children
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => builder.Build(typeof(given.EntriesOrderedByMissingProperty)));

    [Fact] void should_fail_with_invalid_property() => _error.ShouldBeOfExactType<InvalidPropertyForType>();
}
