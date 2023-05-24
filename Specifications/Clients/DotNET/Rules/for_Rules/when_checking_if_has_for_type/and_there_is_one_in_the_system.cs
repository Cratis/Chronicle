// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_Rules.when_checking_if_has_for_type;

public class and_there_is_one_in_the_system : given.one_rule_for_type
{
    bool result;

    void Because() => result = rules.HasFor(typeof(TypeForRules));

    [Fact] void should_have_it() => result.ShouldBeTrue();
}
