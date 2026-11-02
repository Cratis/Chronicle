// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_InferredReadModelCoverage.when_finding_uncovered_targets;

public class and_targets_are_missing_at_multiple_levels : given.a_projection_definition
{
    void Establish()
    {
        _definition.FromEvery.Properties["LastUpdated"] = "$eventContext.occurred";
        var properties = new Dictionary<PropertyPath, string>
        {
            ["IsShipped"] = "false",
            ["Renamed"] = "Name",
            ["Incremented"] = WellKnownExpressions.Increment,
            ["Decremented"] = WellKnownExpressions.Decrement,
            ["Counted"] = WellKnownExpressions.Count,
            ["Added"] = $"{WellKnownExpressions.Add}(Quantity)",
            ["Subtracted"] = $"{WellKnownExpressions.Subtract}(Quantity)"
        };
        _definition.From[EventType] = new(properties, WellKnownExpressions.EventSourceId, null);
        _definition.From[new EventType("ItemRemoved", EventTypeGeneration.First)] = new(properties, WellKnownExpressions.EventSourceId, null);
        _definition.Children["Lines"] = Child();
        _definition.Children["ItemlessLines"] = Child();
        _definition.Children["MissingLines"] = Child();
        _definition = _definition with
        {
            Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["Details"] = Child() }
        };
    }

    void Because() => _uncovered = InferredReadModelCoverage.FindUncoveredTargets(_definition, JsonSchema.FromJson("""
        { "type": "object", "properties": {
            "Lines": { "type": "object" }, "ItemlessLines": { "type": "array" }
          } }
        """));

    [Fact]
    void should_list_targets_in_walk_order_without_duplicates() => _uncovered.ShouldEqual<string>(
    [
        "LastUpdated (every)", "IsShipped", "Renamed", "Incremented (counter)", "Decremented (counter)",
        "Counted (counter)", "Added (counter)", "Subtracted (counter)",
        "Details (nested object)", "Details.Quantity", "Details.ProductName (join)",
        "Lines (child collection)", "Lines.Quantity", "Lines.ProductName (join)",
        "ItemlessLines (child collection)", "ItemlessLines.Quantity", "ItemlessLines.ProductName (join)",
        "MissingLines (child collection)", "MissingLines.Quantity", "MissingLines.ProductName (join)"
    ]);
}
