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

public class with_multiple_from_events : given.a_model_bound_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Name"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Description"] = new JsonSchemaProperty { Type = JsonObjectType.String }
        };

        _readModelDefinition = CreateReadModelDefinition("Product", properties);

        var productCreatedEvent = CreateEventType("ProductCreated");
        var productRenamedEvent = CreateEventType("ProductRenamed");
        var productDescriptionChangedEvent = CreateEventType("ProductDescriptionChanged");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [productCreatedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Name")] = "Name",
                    [new PropertyPath("Description")] = "Description"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null),
            [productRenamedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Name")] = "NewName"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null),
            [productDescriptionChangedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Description")] = "NewDescription"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition(from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_compilation_unit() => _result.ShouldNotBeNull();

    [Fact] void should_have_three_from_event_attributes()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var fromEventAttrs = record.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.Name.ToString().Contains("FromEvent"))
            .ToList();
        fromEventAttrs.Count.ShouldEqual(3);
    }

    [Fact] void should_have_product_created_from_event()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var hasProductCreated = record.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("FromEvent") && a.Name.ToString().Contains("ProductCreated"));
        hasProductCreated.ShouldBeTrue();
    }

    [Fact] void should_have_product_renamed_from_event()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var hasProductRenamed = record.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("FromEvent") && a.Name.ToString().Contains("ProductRenamed"));
        hasProductRenamed.ShouldBeTrue();
    }

    [Fact] void should_have_product_description_changed_from_event()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var hasProductDescriptionChanged = record.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("FromEvent") && a.Name.ToString().Contains("ProductDescriptionChanged"));
        hasProductDescriptionChanged.ShouldBeTrue();
    }
}
