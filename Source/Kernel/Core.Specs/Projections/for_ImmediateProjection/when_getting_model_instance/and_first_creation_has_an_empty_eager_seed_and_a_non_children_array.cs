// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_first_creation_has_an_empty_eager_seed_and_a_non_children_array : for_Projection.given.a_projection_for_first_creation
{
    async Task Because()
    {
        await Materialize(CreateEvent(EventSequenceNumber.First));
        _result = await _immediate.GetModelInstance();
    }

    [Fact] void should_start_with_an_empty_eager_seed() => _eagerSeed.ShouldBeEmpty();
    [Fact] void should_report_the_instance_as_present() => _result.HasReadModel.ShouldBeTrue();
    [Fact] void should_include_the_non_children_array_initial_value() => _result.ReadModel.ToJsonString().ShouldEqual("""{"id":"key","label":"Created","tags":[],"__subject":"00000000-0000-0000-0000-000000000000"}""");
    [Fact] void should_match_the_materialized_pipeline() => JsonNode.DeepEquals(_result.ReadModel, _materialized).ShouldBeTrue();
    [Fact] void should_project_the_creation_event() => _result.ProjectedEventsCount.ShouldEqual(1);
}
