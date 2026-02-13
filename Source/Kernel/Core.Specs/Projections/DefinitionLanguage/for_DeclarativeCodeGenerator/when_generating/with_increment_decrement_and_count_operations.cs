// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_increment_decrement_and_count_operations : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["LoginCount"] = new JsonSchemaProperty { Type = JsonObjectType.Integer },
            ["LogoutCount"] = new JsonSchemaProperty { Type = JsonObjectType.Integer },
            ["EventCount"] = new JsonSchemaProperty { Type = JsonObjectType.Integer }
        };

        _readModelDefinition = CreateReadModelDefinition("UserStatistics", properties);

        var userLoggedInEvent = CreateEventType("UserLoggedIn");
        var userLoggedOutEvent = CreateEventType("UserLoggedOut");
        var someEvent = CreateEventType("SomeEvent");

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
                    [new PropertyPath("LogoutCount")] = WellKnownExpressions.Decrement
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null),
            [someEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("EventCount")] = WellKnownExpressions.Count
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition("UserStatisticsProjection", _readModelDefinition.Identifier, from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();

    [Fact] void should_contain_increment_operation() => _result.ShouldContain(".Increment(m => m.LoginCount)");

    [Fact] void should_contain_decrement_operation() => _result.ShouldContain(".Decrement(m => m.LogoutCount)");

    [Fact] void should_contain_count_operation() => _result.ShouldContain(".Count(m => m.EventCount)");

    [Fact] void should_contain_from_user_logged_in() => _result.ShouldContain(".From<UserLoggedIn>");

    [Fact] void should_contain_from_user_logged_out() => _result.ShouldContain(".From<UserLoggedOut>");

    [Fact] void should_contain_from_some_event() => _result.ShouldContain(".From<SomeEvent>");
}
