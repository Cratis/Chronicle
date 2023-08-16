// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Rules.for_RulesProjections.when_getting;

public class and_it_has_it : given.two_rules_with_projections
{
    ProjectionDefinition result;

    void Because() => result = rules_projections.GetFor(SecondRule.RuleIdentifier);

    [Fact] void should_return_a_definition() => result.ShouldNotBeNull();
    [Fact] void should_return_definition_for_rule() => result.Identifier.ShouldEqual(SecondRule.RuleIdentifier);
}
