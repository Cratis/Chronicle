// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_count_operations : given.a_model_bound_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["TotalOrders"] = new JsonSchemaProperty { Type = JsonObjectType.Integer }
        };

        _readModelDefinition = CreateReadModelDefinition("EventMetrics", properties);

        var orderPlacedEvent = CreateEventType("OrderPlaced");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [orderPlacedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("TotalOrders")] = WellKnownExpressions.Count
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition(from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_compilation_unit() => _result.ShouldNotBeNull();

    [Fact] void should_have_count_attribute()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var totalOrdersParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "TotalOrders");
        var hasCount = totalOrdersParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("Count"));
        hasCount.ShouldBeTrue();
    }

    [Fact] void should_have_order_placed_event_in_count()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var totalOrdersParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "TotalOrders");
        var countAttr = totalOrdersParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Contains("Count"));
        countAttr.Name.ToString().ShouldContain("OrderPlaced");
    }
}
