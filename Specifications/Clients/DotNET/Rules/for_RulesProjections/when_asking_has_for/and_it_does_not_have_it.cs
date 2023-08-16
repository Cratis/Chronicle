// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_RulesProjections.when_asking_has_for;

public class and_it_does_not_have_it : given.a_rules_projections
{
    bool result;

    void Because() => result = rules_projections.HasFor(Guid.NewGuid());

    [Fact] void should_return_false() => result.ShouldBeFalse();
}
