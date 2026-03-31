// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_set_value_operations : given.a_model_bound_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Status"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Priority"] = new JsonSchemaProperty { Type = JsonObjectType.Integer }
        };

        _readModelDefinition = CreateReadModelDefinition("TaskView", properties);

        var taskCreatedEvent = CreateEventType("TaskCreated");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [taskCreatedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Status")] = $"{WellKnownExpressions.Value}(active)",
                    [new PropertyPath("Priority")] = $"{WellKnownExpressions.Value}(1)"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition(from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_compilation_unit() => _result.ShouldNotBeNull();

    [Fact] void should_have_set_value_attribute_for_status()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var statusParam = record.ParameterList.Parameters.First(p => p.Identifier.Text == "Status");
        var hasSetValue = statusParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("SetValue"));
        hasSetValue.ShouldBeTrue();
    }

    [Fact] void should_have_task_created_event_in_set_value_for_status()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var statusParam = record.ParameterList.Parameters.First(p => p.Identifier.Text == "Status");
        var setValueAttr = statusParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Contains("SetValue"));
        setValueAttr.Name.ToString().ShouldContain("TaskCreated");
    }

    [Fact] void should_have_string_literal_for_status_value()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var statusParam = record.ParameterList.Parameters.First(p => p.Identifier.Text == "Status");
        var setValueAttr = statusParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Contains("SetValue"));
        setValueAttr.ArgumentList!.Arguments.Count.ShouldEqual(1);
        setValueAttr.ArgumentList.Arguments[0].ToString().ShouldContain("active");
    }

    [Fact] void should_have_set_value_attribute_for_priority()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var priorityParam = record.ParameterList.Parameters.First(p => p.Identifier.Text == "Priority");
        var hasSetValue = priorityParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("SetValue"));
        hasSetValue.ShouldBeTrue();
    }

    [Fact] void should_have_numeric_literal_for_priority_value()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var priorityParam = record.ParameterList.Parameters.First(p => p.Identifier.Text == "Priority");
        var setValueAttr = priorityParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Contains("SetValue"));
        setValueAttr.ArgumentList!.Arguments.Count.ShouldEqual(1);
        setValueAttr.ArgumentList.Arguments[0].ToString().ShouldEqual("1");
    }
}
