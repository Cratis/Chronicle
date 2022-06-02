// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;

namespace Aksio.Cratis.Applications.Rules.for_Rules;

public class when_getting_projection_definition_for_rule_second_time : given.one_rule_for_type
{
    Rule rule;
    ProjectionDefinition projection;

    void Establish() => rule = new();

    void Because()
    {
        projection = rules.GetProjectionDefinitionFor(rule);
        projection = rules.GetProjectionDefinitionFor(rule);
    }

    [Fact] void should_call_define_state_once() => rule.DefineStateCallCount.ShouldEqual(1);
}
