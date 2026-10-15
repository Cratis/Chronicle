// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_a_never_created_projection_has_schema_defaults : given.a_never_created_projection
{
    async Task Because() => await Read();

    [Fact] void should_report_the_default_valued_instance_as_present() => _result.HasReadModel.ShouldBeTrue();
    [Fact] void should_seed_the_default_name() => _result.ReadModel["name"].ShouldBeNull();
    [Fact] void should_seed_the_default_value() => _result.ReadModel["value"]!.GetValue<int>().ShouldEqual(0);
    [Fact] void should_seed_the_default_enabled_flag() => _result.ReadModel["enabled"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_seed_the_default_children_collection() => _result.ReadModel["children"].ShouldBeNull();
    [Fact] void should_not_assign_the_requested_identifier() => _result.ReadModel["id"].ShouldBeNull();
    [Fact] void should_report_no_projected_events() => _result.ProjectedEventsCount.ShouldEqual(0);
    [Fact] void should_report_no_matching_event() => _result.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_preserve_all_seeded_properties() => _result.ReadModel.ToJsonString().ShouldEqual("""{"value":0,"enabled":false}""");
    [Fact] void should_return_the_same_instance_to_the_client() => JsonNode.DeepEquals(JsonNode.Parse(_clientResult.ReadModel), _result.ReadModel).ShouldBeTrue();
}
