// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_RulesProjections.when_getting;

public class and_it_does_not_have_it : given.two_rules_with_projections
{
    Exception result;

    void Because() => result = Catch.Exception(() => rules_projections.GetFor(Guid.NewGuid()));

    [Fact] void should_throw_missing_projection_for_rule() => result.ShouldBeOfExactType<MissingProjectionForRule>();
}
