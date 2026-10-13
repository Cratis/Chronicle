// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_a_never_created_projection_has_declared_initial_values : given.a_never_created_projection
{
    protected override JsonObject InitialModelState => new() { ["name"] = "Initial", ["value"] = 42 };

    async Task Because() => await Read();

    [Fact] void should_report_the_seeded_instance_as_present() => _result.HasReadModel.ShouldBeTrue();
    [Fact] void should_keep_the_declared_name() => _result.ReadModel["name"]!.GetValue<string>().ShouldEqual("Initial");
    [Fact] void should_keep_the_declared_value() => _result.ReadModel["value"]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_seed_the_remaining_schema_defaults() => _result.ReadModel["enabled"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_return_the_same_instance_to_the_client() => JsonNode.DeepEquals(JsonNode.Parse(_clientResult.ReadModel), _result.ReadModel).ShouldBeTrue();
}
