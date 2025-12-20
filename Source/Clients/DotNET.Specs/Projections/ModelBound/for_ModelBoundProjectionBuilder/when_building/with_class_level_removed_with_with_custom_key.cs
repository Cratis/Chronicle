// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_class_level_removed_with_with_custom_key : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(RemovableReadModelWithKey));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_one_removed_with_entry() => _result.RemovedWith.Count.ShouldEqual(1);
    [Fact] void should_have_removed_with_for_correct_event_type() => _result.RemovedWith.Keys.First().Id.ShouldEqual(event_types.GetEventTypeFor(typeof(ReadModelRemoved)).Id.ToString());
    [Fact] void should_use_specified_key_expression() => _result.RemovedWith.Values.First().Key.ShouldEqual(nameof(ReadModelRemoved.Id));
}
