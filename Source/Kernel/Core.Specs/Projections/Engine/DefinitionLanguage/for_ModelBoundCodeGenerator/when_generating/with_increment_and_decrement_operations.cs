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

public class with_increment_and_decrement_operations : given.a_model_bound_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["LoginCount"] = new JsonSchemaProperty { Type = JsonObjectType.Integer }
        };

        _readModelDefinition = CreateReadModelDefinition("UserStatistics", properties);

        var userLoggedInEvent = CreateEventType("UserLoggedIn");
        var userLoggedOutEvent = CreateEventType("UserLoggedOut");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [userLoggedInEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("LoginCount")] = WellKnownExpressions.Increment
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null),
            [userLoggedOutEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("LoginCount")] = WellKnownExpressions.Decrement
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition(from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_compilation_unit() => _result.ShouldNotBeNull();

    [Fact] void should_have_increment_attribute()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var loginCountParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "LoginCount");
        var hasIncrement = loginCountParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("Increment"));
        hasIncrement.ShouldBeTrue();
    }

    [Fact] void should_have_decrement_attribute()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var loginCountParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "LoginCount");
        var hasDecrement = loginCountParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("Decrement"));
        hasDecrement.ShouldBeTrue();
    }

    [Fact] void should_have_user_logged_in_event_in_increment()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var loginCountParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "LoginCount");
        var incrementAttr = loginCountParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Contains("Increment"));
        incrementAttr.Name.ToString().ShouldContain("UserLoggedIn");
    }

    [Fact] void should_have_user_logged_out_event_in_decrement()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var loginCountParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "LoginCount");
        var decrementAttr = loginCountParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Contains("Decrement"));
        decrementAttr.Name.ToString().ShouldContain("UserLoggedOut");
    }
}
