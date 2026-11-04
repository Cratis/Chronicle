// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_InferredReadModelCoverage.when_finding_uncovered_targets;

public class and_every_target_is_covered : given.a_projection_definition
{
    void Establish()
    {
        _definition.FromEvery.Properties["LastUpdated"] = "$eventContext.occurred";
        _definition.From[EventType] = new(new Dictionary<PropertyPath, string>
        {
            ["CustomerName"] = "CustomerName",
            ["ItemCount"] = WellKnownExpressions.Increment,
            ["Details.Quantity"] = "Quantity",
            ["Lines.Quantity"] = "Quantity",
            ["Source.ProductName"] = "Name"
        },
        WellKnownExpressions.EventSourceId,
        null);
        _definition.Join[EventType] = new("ProductId", new Dictionary<PropertyPath, string> { ["ProductName"] = "Name" }, "ProductId");
        _definition.Children["Lines"] = Child();
        _definition = _definition with
        {
            Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["Details"] = Child() }
        };
    }

    void Because() => _uncovered = InferredReadModelCoverage.FindUncoveredTargets(_definition, JsonSchema.FromJson("""
        { "type": "object", "properties": {
            "lastUpdated": { "type": "string" },
            "CustomerName": { "type": "string" },
            "ItemCount": { "type": "integer" },
            "ProductName": { "type": "string" },
            "Lines": { "type": "array", "items": { "$ref": "#/$defs/Line" } },
            "Details": { "$ref": "#/$defs/Line" }
          }, "$defs": { "Line": { "type": "object", "properties": {
            "Quantity": { "type": "integer" }, "ProductName": { "type": "string" }
          } } } }
        """));

    [Fact] void should_find_nothing() => _uncovered.ShouldBeEmpty();
}
