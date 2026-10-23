// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_Jobs.when_computing_the_role_budget;

public class with_the_observer_cap : Specification
{
    int _result;

    void Because() => _result = new Jobs { MaxParallelSteps = 80 }.GetEffectiveMaxParallelSteps(true, 32);

    [Fact] void should_use_the_expected_budget() => _result.ShouldEqual(32);
}
