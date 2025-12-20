// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_class_level_removed_with_and_removed_with_join : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(ReadModelWithMultipleRemovalOptions));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_one_removed_with_entry() => _result.RemovedWith.Count.ShouldEqual(1);
    [Fact] void should_have_one_removed_with_join_entry() => _result.RemovedWithJoin.Count.ShouldEqual(1);
    [Fact] void should_have_removed_with_for_correct_event_type() => _result.RemovedWith.Keys.First().Id.ShouldEqual(event_types.GetEventTypeFor(typeof(ReadModelRemoved)).Id.ToString());
    [Fact] void should_have_removed_with_join_for_correct_event_type() => _result.RemovedWithJoin.Keys.First().Id.ShouldEqual(event_types.GetEventTypeFor(typeof(ReadModelRemovedJoin)).Id.ToString());
}
