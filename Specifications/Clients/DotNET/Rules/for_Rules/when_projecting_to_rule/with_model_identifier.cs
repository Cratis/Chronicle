// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Properties;
using Aksio.Strings;

namespace Aksio.Cratis.Rules.for_Rules.when_projecting_to_rule;

public class with_model_identifier : given.no_rules
{
    const string first_state_value = "Forty two";
    const int second_state_value = 42;
    const int complex_state_some_integer = 43;
    const string complex_state_some_string = "Forty three";

    const string model_identifier = "282c491b-10a9-4ec0-ae23-659c4e6aaf16";

    RuleWithState rule;

    void Establish()
    {
        rule = new();
        rules_projections.Setup(_ => _.HasFor(rule.Identifier)).Returns(true);

        var jsonObject = new JsonObject
        {
            [nameof(RuleWithState.FirstStateValue).ToCamelCase()] = first_state_value,
            [nameof(RuleWithState.SecondStateValue).ToCamelCase()] = second_state_value,
            [nameof(RuleWithState.ComplexState).ToCamelCase()] = new JsonObject
            {
                [nameof(ComplexState.SomeInteger).ToCamelCase()] = complex_state_some_integer,
                [nameof(ComplexState.SomeString).ToCamelCase()] = complex_state_some_string
            }
        };

        immediate_projections
            .Setup(_ => _.GetInstanceById(rule.Identifier.Value, IsAny<ModelKey>()))
            .Returns(Task.FromResult(new ImmediateProjectionResultRaw(jsonObject, Enumerable.Empty<PropertyPath>(), 0)));
    }

    void Because() => rules.ProjectTo(rule, model_identifier);

    [Fact] void should_set_first_state_value() => rule.FirstStateValue.ShouldEqual(first_state_value);
    [Fact] void should_set_second_state_value() => rule.SecondStateValue.ShouldEqual(second_state_value);
    [Fact] void should_set_complex_state() => rule.ComplexState.ShouldEqual(new ComplexState(complex_state_some_integer, complex_state_some_string));
}
