// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.when_asking_if_has_for_name;

public class and_it_has_it : given.two_constraints
{
    bool _result;

    async Task Establish()
    {
        await _constraints.Discover();
    }

    void Because() => _result = _constraints.HasFor(_secondConstraintName);

    [Fact] void should_have_the_constraint() => _result.ShouldBeTrue();
}
