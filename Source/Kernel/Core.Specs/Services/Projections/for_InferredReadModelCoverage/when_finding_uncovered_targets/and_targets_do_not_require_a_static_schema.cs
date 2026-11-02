// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_InferredReadModelCoverage.when_finding_uncovered_targets;

public class and_targets_do_not_require_a_static_schema : given.a_projection_definition
{
    void Establish() => _definition.From[EventType] = new(new Dictionary<PropertyPath, string>
    {
        [PropertyPath.NotSet] = "Name",
        [PropertyPath.Root] = "Name",
        [PropertyPath.ThisAccessorValue] = "Name",
        ["Values.$key"] = "Name"
    },
    WellKnownExpressions.EventSourceId,
    null);

    void Because() => _uncovered = InferredReadModelCoverage.FindUncoveredTargets(_definition, JsonSchema.FromJson("""{ "type": "object" }"""));

    [Fact] void should_find_nothing() => _uncovered.ShouldBeEmpty();
}
