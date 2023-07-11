// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_Rules.when_checking_if_has_for_type;

public class and_there_is_none_in_the_system : given.no_rules
{
    bool result;

    void Because() => result = rules.HasFor(typeof(object));

    [Fact] void should_not_have_it() => result.ShouldBeFalse();
}
