// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Strings;

namespace Cratis.Chronicle.Rules.for_Rules.when_projecting_to_rule;

public class with_no_model_identifier : given.no_rules
{
    const string FirstStateValue = "Forty two";
    const int SecondStateValue = 42;
    const int ComplexStateSomeInteger = 43;
    const string ComplexStateSomeSring = "Forty three";

    RuleWithState _rule;

    void Establish()
    {
        _rule = new();
        _projections.HasFor(_rule.GetRuleId().Value).Returns(true);

        var jsonObject = new JsonObject
        {
            [nameof(RuleWithState.FirstStateValue).ToCamelCase()] = FirstStateValue,
            [nameof(RuleWithState.SecondStateValue).ToCamelCase()] = SecondStateValue,
            [nameof(RuleWithState.ComplexState).ToCamelCase()] = new JsonObject
            {
                [nameof(ComplexState.SomeInteger).ToCamelCase()] = ComplexStateSomeInteger,
                [nameof(ComplexState.SomeString).ToCamelCase()] = ComplexStateSomeSring
            }
        };

        _projections
            .GetInstanceById(_rule.GetRuleId().Value, Arg.Any<ReadModelKey>())
            .Returns(Task.FromResult(new ProjectionResultRaw(jsonObject, [], 0, 42)));
    }

    void Because() => rules.ProjectTo(_rule);

    [Fact] void should_set_first_state_value() => _rule.FirstStateValue.ShouldEqual(FirstStateValue);
    [Fact] void should_set_second_state_value() => _rule.SecondStateValue.ShouldEqual(SecondStateValue);
    [Fact] void should_set_complex_state() => _rule.ComplexState.ShouldEqual(new ComplexState(ComplexStateSomeInteger, ComplexStateSomeSring));
}
