// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_RulesProjections.when_asking_has_for;

public class and_it_has_it : given.two_rules_with_projections
{
    bool result;

    void Because() => result = rules_projections.HasFor(SecondRule.RuleIdentifier);

    [Fact] void should_return_true() => result.ShouldBeTrue();
}
