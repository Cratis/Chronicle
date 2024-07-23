// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Strings;

namespace Cratis.Chronicle.Rules.for_Rules.when_projecting_to_rule;

public class with_no_model_identifier : given.no_rules
{
    const string first_state_value = "Forty two";
    const int second_state_value = 42;
    const int complex_state_some_integer = 43;
    const string complex_state_some_string = "Forty three";

    RuleWithState rule;

    void Establish()
    {
        rule = new();
        rules_projections.Setup(_ => _.HasFor(rule.GetRuleId())).Returns(true);

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

        projections
            .Setup(_ => _.GetInstanceById(rule.GetRuleId().Value, IsAny<ModelKey>()))
            .Returns(Task.FromResult(new ImmediateProjectionResultRaw(jsonObject, [], 0)));
    }

    void Because() => rules.ProjectTo(rule);

    [Fact] void should_set_first_state_value() => rule.FirstStateValue.ShouldEqual(first_state_value);
    [Fact] void should_set_second_state_value() => rule.SecondStateValue.ShouldEqual(second_state_value);
    [Fact] void should_set_complex_state() => rule.ComplexState.ShouldEqual(new ComplexState(complex_state_some_integer, complex_state_some_string));
}
