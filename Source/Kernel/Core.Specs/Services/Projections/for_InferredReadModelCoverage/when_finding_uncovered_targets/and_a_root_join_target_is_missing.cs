// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_InferredReadModelCoverage.when_finding_uncovered_targets;

public class and_a_root_join_target_is_missing : given.a_projection_definition
{
    void Establish() => _definition.Join[EventType] = new(
        "ProductId",
        new Dictionary<PropertyPath, string> { ["ProductName"] = "Name" },
        "ProductId");

    void Because() => _uncovered = InferredReadModelCoverage.FindUncoveredTargets(_definition, JsonSchema.FromJson("""{ "type": "object" }"""));

    [Fact] void should_name_the_join_target() => _uncovered.ShouldContainOnly("ProductName (join)");
}
