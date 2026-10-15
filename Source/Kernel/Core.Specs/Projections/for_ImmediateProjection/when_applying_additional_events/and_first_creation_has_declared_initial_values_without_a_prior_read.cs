// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_applying_additional_events;

public class and_first_creation_has_declared_initial_values_without_a_prior_read : for_Projection.given.a_projection_for_first_creation
{
    protected override bool HasDeclaredValues => true;
    protected override JsonObject InitialModelState => new() { ["name"] = "Initial", ["value"] = 42 };

    async Task Because()
    {
        await Materialize(CreateEvent(EventSequenceNumber.Unavailable));
        _error = await Catch.Exception(async () => _result = await _immediate.GetCurrentModelInstanceWithAdditionalEventsApplied(
            [new(_created, new JsonObject { ["key"] = "key", ["label"] = "Created" })]));
    }

    [Fact] void should_not_require_a_prior_read() => _error.ShouldBeNull();
    [Fact] void should_report_the_instance_as_present() => (_result?.HasReadModel == true).ShouldBeTrue();
    [Fact] void should_include_declared_initial_values() => JsonNode.DeepEquals(_result?.ReadModel, JsonNode.Parse("""{"id":"key","name":"Initial","value":42,"label":"Created","__subject":"00000000-0000-0000-0000-000000000000"}""")).ShouldBeTrue();
    [Fact] void should_match_the_materialized_pipeline() => JsonNode.DeepEquals(_result?.ReadModel, _materialized).ShouldBeTrue();
    [Fact] void should_keep_the_materialized_initial_values() => _materialized.ToJsonString().ShouldEqual("""{"id":"key","name":"Initial","label":"Created","value":42,"__subject":"00000000-0000-0000-0000-000000000000"}""");
}
